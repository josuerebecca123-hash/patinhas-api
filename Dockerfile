FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copia só o .csproj primeiro para aproveitar o cache do restore.
COPY Patinhas.Api/Patinhas.Api.csproj Patinhas.Api/
RUN dotnet restore Patinhas.Api/Patinhas.Api.csproj

COPY Patinhas.Api/ Patinhas.Api/
RUN dotnet publish Patinhas.Api/Patinhas.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=10000
EXPOSE 10000
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "Patinhas.Api.dll"]
