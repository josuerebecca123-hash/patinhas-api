using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Patinhas.Api.Dados;
using Patinhas.Api.Seguranca;
using Patinhas.Api.Servicos;

var builder = WebApplication.CreateBuilder(args);

// ---------- Banco (PostgreSQL / Neon) ----------
var connectionString = builder.Configuration.GetConnectionString("Padrao");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException(
        "A connection string 'Padrao' não foi configurada. Defina a variável de ambiente ConnectionStrings__Padrao.");

builder.Services.AddDbContext<PatinhasDbContext>(opcoes => opcoes.UseNpgsql(connectionString));

// ---------- JWT ----------
var segredoJwt = builder.Configuration["Jwt:Segredo"];
if (string.IsNullOrWhiteSpace(segredoJwt) || segredoJwt.Length < 32)
    throw new InvalidOperationException(
        "O segredo do JWT não foi configurado ou tem menos de 32 caracteres. Defina a variável de ambiente Jwt__Segredo.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opcoes =>
    {
        opcoes.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = JwtConstantes.Emissor,
            ValidateAudience = true,
            ValidAudience = JwtConstantes.Audiencia,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(segredoJwt)),
            ClockSkew = TimeSpan.Zero,
            NameClaimType = ClaimTypes.NameIdentifier,
            RoleClaimType = ClaimTypes.Role
        };
    });
builder.Services.AddAuthorization();

// ---------- CORS ----------
var origensConfiguradas = builder.Configuration["Cors:Origens"];
var origens = (string.IsNullOrWhiteSpace(origensConfiguradas) ? "http://localhost:5173" : origensConfiguradas)
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddCors(opcoes => opcoes.AddPolicy("Front", politica => politica
    .WithOrigins(origens)
    .AllowAnyHeader()
    .AllowAnyMethod()
    .WithExposedHeaders("Location")));

// ---------- Erros, controllers, health ----------
builder.Services.AddProblemDetails();
builder.Services.AddControllers();
builder.Services.AddScoped<IAuthServico, AuthServico>();
builder.Services.AddScoped<ICatalogoServicos, CatalogoServicos>();
builder.Services.AddHealthChecks().AddDbContextCheck<PatinhasDbContext>();

// ---------- Swagger (Swashbuckle 10 / OpenAPI.NET 2.x) ----------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opcoes =>
{
    opcoes.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Patinhas API",
        Version = "v1",
        Description = "Projeto escolar — empresa fictícia"
    });

    opcoes.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Cole apenas o token JWT (sem o prefixo 'Bearer ')."
    });

    opcoes.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
    {
        { new OpenApiSecuritySchemeReference("Bearer", doc), new List<string>() }
    });
});

var app = builder.Build();

// ---------- Migrations + seed ----------
using (var escopo = app.Services.CreateScope())
{
    var db = escopo.ServiceProvider.GetRequiredService<PatinhasDbContext>();
    await db.Database.MigrateAsync();
    await Seed.ExecutarAsync(db, app.Configuration, app.Logger);
}

// ---------- Pipeline ----------
app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseSwagger();
app.UseSwaggerUI(opcoes => opcoes.SwaggerEndpoint("/swagger/v1/swagger.json", "Patinhas API v1"));

app.UseCors("Front"); // antes de UseAuthentication
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
app.MapHealthChecks("/health");
app.MapGet("/api/versao", () => new
{
    commit = Environment.GetEnvironmentVariable("RENDER_GIT_COMMIT") ?? "local",
    ambiente = app.Environment.EnvironmentName
}).WithTags("Sistema");
app.MapControllers();

app.Run();

public partial class Program { }
