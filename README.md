# Veyra API

API REST em ASP.NET Core com autenticação JWT, refresh token rotativo e autorização por roles.

## Stack

- .NET 10 (`net10.0`)
- ASP.NET Core — controllers
- `Microsoft.AspNetCore.Authentication.JwtBearer` — validação do access token
- OpenAPI nativo do .NET 10 (`AddOpenApi`, sem Swashbuckle)
- xUnit

Único pacote NuGet além do OpenAPI é o `JwtBearer`. O hash de senha usa
`PasswordHasher<T>`, que já vem no shared framework do ASP.NET Core.

## Rodando

Requer o **SDK do .NET 10**.

```bash
dotnet restore
dotnet run --project Veyra.Api
```

A API sobe em `http://localhost:5217` (perfil `http`) ou `https://localhost:7228`
(perfil `https`). Em Development o documento OpenAPI fica em `/openapi/v1.json`.

Testes:

```bash
dotnet test
```

## Endpoints

| Método | Rota | Acesso | Descrição |
|---|---|---|---|
| `GET` | `/status` | público | Health check |
| `POST` | `/api/auth/register` | público | Cadastro; cria sempre com role `CLIENT` |
| `POST` | `/api/auth/login` | público | Devolve access token + refresh token |
| `POST` | `/api/auth/refresh` | público | Rotaciona o par de tokens |
| `POST` | `/api/auth/logout` | autenticado | Revoga o refresh token apresentado |
| `GET` | `/api/auth/me` | autenticado | Dados do usuário do token atual |
| `GET` | `/api/users` | autenticado | Lista usuários |
| `GET` | `/api/users/{id}` | autenticado | Busca por id |
| `DELETE` | `/api/users/{id}` | role `ADMIN` | Remove usuário |

O arquivo [`Veyra.Api/Veyra.Api.http`](Veyra.Api/Veyra.Api.http) tem o fluxo completo
pronto para executar, incluindo os casos de 401, 403 e detecção de reuso.

### Login

```http
POST /api/auth/login
Content-Type: application/json

{ "email": "daniel@veyra.local", "password": "senha@12345" }
```

```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "refreshToken": "3Qm7xK...",
  "expiresIn": 900,
  "tokenType": "Bearer",
  "user": { "id": 1, "name": "Daniel", "email": "daniel@veyra.local", "role": "CLIENT" }
}
```

Nas rotas protegidas, mande `Authorization: Bearer <accessToken>`.

## Como a autenticação funciona

**Access token** — JWT assinado com HMAC-SHA256, validade de 15 minutos. Carrega as claims
`sub` (id), `email`, `name`, `role` e `jti`. A validação confere issuer, audience, assinatura
e expiração, com `ClockSkew` zerado — o padrão de 5 minutos do framework faria um token
expirado continuar sendo aceito.

**Refresh token** — valor opaco de 256 bits gerado por CSPRNG (não é JWT), validade de 7 dias.

### Rotação com detecção de reuso

Cada `/refresh` **consome** o token apresentado e emite um par novo. Um refresh token nunca
serve duas vezes.

Se alguém apresenta um token que **já foi consumido**, isso significa que existem duas cópias
dele circulando — sinal de vazamento. A resposta é revogar **toda a família de tokens do
usuário**, derrubando as duas sessões:

```
login          → refresh A
POST /refresh A → 200, refresh B   (A morre aqui)
POST /refresh A → 401 + B também é revogado
POST /refresh B → 401
```

Sem esse passo, um atacante com um token roubado manteria acesso indefinidamente e em silêncio.
Com ele, o ataque derruba a sessão da vítima — que percebe e faz login de novo, invalidando o
que o atacante tem.

Expiração natural do token **não** dispara isso: é comportamento normal de cliente, não sinal
de ataque.

O consumo é um compare-and-set atômico, não um "checar depois revogar" — dois `/refresh`
simultâneos com o mesmo token não podem gerar duas sessões válidas.

### Roles

`UserEnum` define `Client`, `Salesperson` e `Admin`. A claim `role` vai em **maiúsculo**
(`CLIENT`, `SALESPERSON`, `ADMIN`), batendo com os atributos `[Authorize(Roles = "ADMIN")]`.

Isso não é cosmético: `[Authorize(Roles = ...)]` compara o valor da claim com `Ordinal`, ou
seja, **diferencia maiúsculas**. Emitir `"Admin"` contra um atributo `"ADMIN"` daria 403 em
admins legítimos, sem erro nenhum no log. Por isso `MapInboundClaims` está desligado e
`RoleClaimType` fixado em `"role"` — do contrário o handler reescreve as claims para as URIs
longas do schema WS-\* e a autorização quebra em silêncio.

O cadastro público sempre cria `CLIENT`. Não há endpoint para promover ninguém: em Development
um admin é semeado no boot (veja abaixo).

## Configuração

Seção `Jwt` do `appsettings.json`:

| Chave | Padrão | Descrição |
|---|---|---|
| `Jwt:Issuer` | `veyra-api` | Emissor |
| `Jwt:Audience` | `veyra-client` | Destinatário |
| `Jwt:Key` | *(vazio)* | Chave de assinatura, mínimo 32 bytes |
| `Jwt:AccessTokenMinutes` | `15` | Validade do access token |
| `Jwt:RefreshTokenDays` | `7` | Validade do refresh token |

**Em produção**, forneça a chave por variável de ambiente — nunca commitada:

```bash
export Jwt__Key="$(openssl rand -base64 48)"
```

ou via user-secrets. A aplicação **se recusa a subir** se a chave estiver ausente ou tiver menos
de 32 bytes (mínimo do HMAC-SHA256), com uma mensagem dizendo como configurá-la. É de propósito:
uma chave fraca falharia só na primeira emissão de token, já em produção.

Em Development há uma chave de desenvolvimento explícita no `appsettings.Development.json`, para
`dotnet run` funcionar sem setup. Ela é claramente marcada como não-produtiva.

### Admin de desenvolvimento

Também em `appsettings.Development.json`, a seção `Seed:Admin` cria um administrador no boot:

```
admin@veyra.local / admin@12345
```

Sem ele não haveria como exercitar `DELETE /api/users/{id}`, já que o cadastro público só gera
`CLIENT` e o armazenamento é volátil.

## Estrutura

```
Veyra.Api/
  Domain/           entidades (User, RefreshToken), enums e as interfaces de repositório
  Application/      serviços (AuthService, UserService), mapper e segurança (TokenService, JwtSettings)
  Infrastructure/   repositórios em memória, transformers de OpenAPI, seed
  Presentation/     controllers e DTOs de request/response
Veyra.Api.Tests/    xUnit
```

## Limitações conhecidas

**Não há banco de dados.** Usuários e refresh tokens vivem em memória e **somem quando a
aplicação reinicia**. Isso é deliberado nesta etapa, não um descuido.

O caminho de migração já está preparado: os serviços dependem de `IUserRepository` e
`IRefreshTokenRepository` ([`Domain/Repository/`](Veyra.Api/Domain/Repository)), e as
implementações atuais em [`Infrastructure/Repository/`](Veyra.Api/Infrastructure/Repository)
são a única coisa que precisa ser trocada por EF Core. Ao fazer isso, dois ajustes valem a pena:

- guardar o **SHA-256** do refresh token em vez do valor em claro (em memória isso não
  agregaria nada, mas contra dump de banco sim);
- envolver a rotação — revogar o antigo e gravar o novo — em uma única transação.
