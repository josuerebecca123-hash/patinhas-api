# Patinhas API

**Nicho:** Pet shop ** · **Aluno:** Rebecca Braga (@josuerebecca123-hash)

**API no ar:** https://patinhas-api-dzmy.onrender.com/swagger
**Saúde e versão:** https://patinhas-api-dzmy.onrender.com/health · https://patinhas-api-dzmy.onrender.com/api/versao
**Front-end:** **[PREENCHER: https://SEU-FRONT.onrender.com]**

> Projeto escolar com empresa fictícia. Não use e-mail nem senha de verdade.
> A API roda na instância gratuita da Render, que dorme após 15 minutos sem uso: o primeiro acesso pode levar cerca de 1 minuto.

## O que a API faz

A Patinhas é a API de um pet shop que vende serviços (banho, tosa, consulta veterinária, hotelzinho) e recebe agendamentos dos clientes.

- **Cliente** cria conta, vê o catálogo de serviços (público), agenda um serviço para o seu pet, consulta e cancela os próprios agendamentos.
- **Admin** cadastra, edita e exclui serviços e enxerga os agendamentos de todos.

**A regra de negócio que devolve 409:** dois agendamentos marcados do **mesmo serviço** não podem ter horários sobrepostos, considerando a duração do serviço. Um banho de 60 minutos às 14h bloqueia um novo agendamento às 14h30, mas não às 15h (encostar não é sobrepor). Agendamento cancelado não bloqueia o horário. A verificação roda dentro de uma transação `Serializable`, para que dois pedidos simultâneos não passem juntos. Outras regras com 409: e-mail já cadastrado, cancelar um agendamento já cancelado e excluir um serviço que já tem agendamentos.

## Recursos e endpoints

| Verbo | Rota | Quem pode | Respostas |
|---|---|---|---|
| GET | /health | todos | 200 |
| GET | /api/versao | todos | 200 |
| POST | /api/auth/registro | todos | 201, 400, 409 |
| POST | /api/auth/login | todos | 200, 400, 401 |
| GET | /api/servicos?busca=&pagina=&tamanho= | todos (admin com token vê também os inativos) | 200 |
| GET | /api/servicos/{id} | todos | 200, 404 |
| POST | /api/servicos | admin | 201, 400, 401, 403 |
| PUT | /api/servicos/{id} | admin | 200, 400, 401, 403, 404 |
| DELETE | /api/servicos/{id} | admin | 204, 401, 403, 404, 409 |
| GET | /api/agendamentos?pagina=&tamanho= | logado (cliente vê os seus; admin vê todos) | 200, 401 |
| GET | /api/agendamentos/{id} | dono ou admin | 200, 401, 403, 404 |
| POST | /api/agendamentos | logado | 201, 400, 401, 409 |
| POST | /api/agendamentos/{id}/cancelar | dono ou admin | 200, 401, 403, 404, 409 |

Erros voltam sempre em `ProblemDetails` (JSON). A listagem pública devolve `{ itens, pagina, tamanho, total, totalPaginas }`.

## Rotas para o corretor testar (sem token)

- Listagem paginada: https://patinhas-api-dzmy.onrender.com/api/servicos?pagina=1&tamanho=5
- Busca (exemplo que encontra algo): https://patinhas-api-dzmy.onrender.com/api/servicos?busca=banho
- Busca sem resultado (200, lista vazia, total 0): https://patinhas-api-dzmy.onrender.com/api/servicos?busca=zzzz
- Detalhe: https://patinhas-api-dzmy.onrender.com/api/servicos/1
- Detalhe inexistente (404): https://patinhas-api-dzmy.onrender.com/api/servicos/9999
- Protegida (401 sem token): https://patinhas-api-dzmy.onrender.com/api/agendamentos

## Contas de teste (fictícias)

- admin: admin@patinhas.com.br / **[PREENCHER: a senha de teste do admin, a mesma de Seed__AdminSenha]**
- cliente A (Marina): marina.souza@exemplo.com / Patinhas@123
- cliente B (Carlos): carlos.lima@exemplo.com / Patinhas@123

Cada cliente já tem um agendamento criado pelo seed. Contas e senhas existem só para teste.

## Banco de dados

PostgreSQL no Neon, acessado pelo EF Core com Npgsql. As migrations ficam em `Patinhas.Api/Migrations/` e são aplicadas na subida da API (`MigrateAsync`), seguidas de um seed idempotente (1 admin, 2 clientes, 10 serviços e 2 agendamentos).

| Entidade | Campos principais |
|---|---|
| **Usuario** | Id, Email (único), SenhaHash (BCrypt), Papel (`admin` ou `cliente`) |
| **Servico** | Id, Nome, Descricao, Preco, DuracaoMinutos, Ativo |
| **Agendamento** | Id, ServicoId, UsuarioId (o dono), NomeDoPet, DataHora (UTC), Status (`Marcado` ou `Cancelado`) |

Relações 1 para N: um **Servico** tem vários **Agendamentos**, e um **Usuario** tem vários **Agendamentos**. As chaves estrangeiras usam `Restrict`, então nada é apagado em cascata.

**Onde está a regra do 409:** `Patinhas.Api/Servicos/AgendamentoServico.cs` (método `CriarAsync`), que usa a regra pura `Agendamento.HaSobreposicao` de `Patinhas.Api/Dominio/Agendamento.cs`. A checagem de dono-ou-admin está no mesmo serviço (`PodeAcessar`), e a identidade do usuário vem sempre dos claims do token.

## Como rodar no computador

Pré-requisitos: .NET SDK 10 e a ferramenta `dotnet-ef` 10.0.12.

Variáveis de ambiente (os valores **não** vão para o repositório; defina no terminal):

| Variável | Para que serve |
|---|---|
| `ConnectionStrings__Padrao` | connection string do PostgreSQL, no formato Npgsql (`Host=...;Database=...;Username=...;Password=...;SSL Mode=VerifyFull;Channel Binding=Require`) |
| `Jwt__Segredo` | segredo para assinar o JWT (32 caracteres ou mais) |
| `Seed__AdminSenha` | senha da conta admin criada pelo seed |
| `Cors__Origens` | (opcional) origens liberadas no CORS, separadas por vírgula; o padrão é `http://localhost:5173` |

```powershell
dotnet build -warnaserror
dotnet test
dotnet run --project Patinhas.Api
```

Para criar uma nova migration, com as variáveis definidas: `dotnet ef migrations add NomeDaMigration --project Patinhas.Api --output-dir Migrations`.

O arquivo `requisicoes.http` (extensão REST Client do VS Code) cobre os fluxos felizes e de erro: login, 201, 400, 401, 403, 404 e 409.

**Deploy:** a cada push no `main`, o GitHub Actions compila (`-warnaserror`) e testa, e a Render refaz o contêiner a partir do `Dockerfile`. O `/api/versao` mostra o hash do commit que está no ar.

## Front-end

**[PREENCHER depois de publicar o front: o que ele faz, as telas (vitrine com busca, login, agendamentos do cliente) e a variável `Cors__Origens` configurada na API com a URL do front.]**

## Como usei a IA

- **IA usada:** **[PREENCHER: confirme quais IAs usou, por exemplo Claude (claude.ai) para a API e a IA do front]**
- **Os 3 pedidos (prompts) que mais ajudaram:**
  1. **[PREENCHER: o prompt inicial do Passo 6, adaptado para o pet shop (copie o seu)]**
  2. **[PREENCHER: o pedido do catálogo de serviços (listagem paginada com busca e CRUD só para admin)]**
  3. **[PREENCHER: o pedido dos agendamentos (regra do 409 em transação e checagem de dono-ou-admin)]**
- **Um erro da IA que percebi e como corrigi:** em `Patinhas.Api/Dtos/ServicoDtos.cs`, a IA escreveu o limite de preço como `[Range(typeof(decimal), "0.01", "100000", ...)]`. Esse formato converte os textos usando a cultura do computador; no meu Windows em português do Brasil, o `0.01` virou um formato inválido e 5 testes de validação do DTO falharam com `FormatException`. Corrigi trocando por números: `[Range(0.01, 100000, ErrorMessage = "...")]`, que não depende da cultura.
- **O prompt usado para gerar o front:** **[PREENCHER depois de gerar o front]**
- **O que aprendi:** **[PREENCHER com as suas palavras: por exemplo, por que a identidade vem dos claims e não do corpo da requisição, por que a regra do 409 precisa de transação, e o que o teste do DTO mostrou sobre confiar no código da IA sem testar]**
