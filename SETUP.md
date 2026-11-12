# Recoup — Local Setup Guide

This guide describes how to run the repository in a local development environment, use the fictional-data presentation mode, and optionally connect the backend to PayPal Sandbox and an AI provider.

> **Security notice:** Do not copy credentials from configuration files into this guide, terminal transcripts, screenshots, or source control. Supply secrets through .NET User Secrets or environment variables. The inspected archive contains a local development settings file with credential-shaped values. Treat any real-looking key found there as exposed: revoke/rotate it with its provider, remove it from the working tree and Git history as appropriate, and replace it with a placeholder or local-only secret store before publishing the repository.

## 1. Prerequisites

| Dependency | Required version / notes |
|---|---|
| Git | Recent version |
| Node.js | A version supported by Vite 8; use a current Node.js LTS release compatible with Vite 8 |
| npm | Bundled with Node.js; `package-lock.json` is provided |
| .NET SDK | .NET 8 SDK |
| Docker Engine / Docker Desktop | Required for the supplied Compose database/API setup |
| Docker Compose | Compose v2 (`docker compose`) |
| PostgreSQL | PostgreSQL 17 with pgvector when running the database outside Docker |
| PayPal Developer account | Optional; required only for real Sandbox synchronization |
| OpenAI API key | Optional; required for OpenAI-backed AI and semantic embeddings |
| Google AI Studio / Gemini API key | Optional; required for Gemini-backed AI |

The backend targets `net8.0`. The API Dockerfile uses the .NET 8 SDK and ASP.NET Core 8 runtime images. The frontend package manifest declares React 19, TypeScript 7, and Vite 8. The repository does not pin a specific Node.js patch/minor version; verify compatibility against Vite 8's current engine requirements.

## 2. Clone the repository

```bash
git clone https://github.com/bugradaryal/paypal-revenue-intelligence.git
cd paypal-revenue-intelligence
```

If you received the project as a ZIP, extract it and change into the `paypal-revenue-intelligence` directory instead.

## 3. Project structure

```text
.
├── backend/
│   ├── PayPalRevenueIntelligence.sln
│   ├── Directory.Build.props
│   ├── src/
│   │   ├── PRI.Api/              # ASP.NET Core controllers, middleware, startup
│   │   ├── PRI.Application/      # Use cases, DTOs, validators, calculations
│   │   ├── PRI.Domain/           # Domain entities, rules, interfaces
│   │   ├── PRI.Infrastructure/   # EF Core, PostgreSQL, PayPal and AI providers
│   │   └── PRI.Shared/           # Shared constants and types
│   └── tests/
│       ├── PRI.AiGateway.Tests/
│       ├── PRI.Api.IntegrationTests/
│       └── PRI.Revenue.Tests/
├── frontend/
│   ├── src/
│   │   ├── features/             # Feature-specific UI and queries
│   │   ├── pages/                # Application pages
│   │   ├── services/adapters/    # Mock and real API adapters
│   │   └── config/env.ts         # Frontend environment configuration
│   ├── scripts/check-i18n.mjs
│   ├── package.json
│   └── package-lock.json
├── local-docker-compose.yml      # Actual Compose filename in this repository
└── README.md
```

The repository snapshot does not include the `docs/API_CONTRACT.md` or `docker-compose.yml` paths referenced by the original README. The Compose file that is actually present is `local-docker-compose.yml`.

## 4. Configure PostgreSQL and pgvector

### Option A — Docker Compose (recommended)

The supplied Compose file starts:

- `pgvector/pgvector:pg17` as PostgreSQL 17 with pgvector.
- The API container built from `backend/src/PRI.Api/Dockerfile`.
- Named volumes for database data and API-protected credential/Data Protection files.

Create a private root `.env` file beside `local-docker-compose.yml`:

```dotenv
POSTGRES_DB=pri
POSTGRES_USER=pri
POSTGRES_PASSWORD=REPLACE_WITH_A_LONG_RANDOM_DATABASE_PASSWORD

POSTGRES_PORT=5432
API_PORT=5000

JWT_KEY=REPLACE_WITH_AT_LEAST_32_RANDOM_CHARACTERS
SEED_ADMIN_EMAIL=admin@pri.local
SEED_ADMIN_PASSWORD=REPLACE_WITH_A_LONG_RANDOM_ADMIN_PASSWORD
SEED_ADMIN_DISPLAY_NAME=Recoup Administrator
```

Generate strong secrets locally. For example, on systems with OpenSSL:

```bash
openssl rand -base64 48
```

Use separate values for the database password, JWT signing key, and administrator password. Do not commit `.env`.

Start the stack from the repository root:

```bash
docker compose -f local-docker-compose.yml up --build -d
```

Check service status and logs:

```bash
docker compose -f local-docker-compose.yml ps
docker compose -f local-docker-compose.yml logs -f api
docker compose -f local-docker-compose.yml logs -f postgres
```

The database health check gates API startup. The Compose API service sets `Database__InitializeOnStartup=true`, so the API applies the checked-in EF Core migrations and seeds the initial administrator and AI gateway catalog when it starts.

The API container listens on port `10000`; Compose maps it to `127.0.0.1:${API_PORT:-5000}`. The database is bound to `127.0.0.1:${POSTGRES_PORT:-5432}`.

Stop the services:

```bash
docker compose -f local-docker-compose.yml down
```

Stop and delete the database volume only when you intentionally want to discard local data:

```bash
docker compose -f local-docker-compose.yml down -v
```

### Option B — Existing local PostgreSQL

Install PostgreSQL 17 and the pgvector extension, then create a database and user. The project uses Npgsql and the EF Core model includes vector-backed semantic-cache storage. The repository does not include a standalone SQL bootstrap script; use a PostgreSQL/pgvector installation compatible with the checked-in migrations.

A conventional connection string has this form:

```text
Host=localhost;Port=5432;Database=pri;Username=pri;Password=REPLACE_ME
```

The backend resolves the database connection in this order:

1. `ConnectionStrings:Default` (environment variable form: `ConnectionStrings__Default`)
2. `DATABASE_URL` as a PostgreSQL URI (`postgres://` or `postgresql://`) or a supported Npgsql connection string value

For local .NET execution, set `ConnectionStrings__Default` or use the development configuration file locally (never commit credentials).

## 5. Root and backend configuration

ASP.NET Core configuration supports environment-variable overrides using double underscores (`__`) for nested keys.

### Docker Compose root `.env` variables

| Variable | Required? | Purpose | Example |
|---|---:|---|---|
| `POSTGRES_DB` | Optional | Database name passed to the PostgreSQL container and API connection string. Defaults to `pri`. | `pri` |
| `POSTGRES_USER` | Optional | PostgreSQL username. Defaults to `pri`. | `pri` |
| `POSTGRES_PASSWORD` | **Required by Compose** | PostgreSQL password. | `use-a-unique-secret` |
| `POSTGRES_PORT` | Optional | Host loopback port mapped to container port 5432. Defaults to `5432`. | `5432` |
| `API_PORT` | Optional | Host loopback port mapped to API container port 10000. Defaults to `5000`. | `5000` |
| `JWT_KEY` | **Required by Compose** | JWT signing key injected as `Jwt:Key`; backend requires at least 32 characters. | `generate-a-long-random-value` |
| `SEED_ADMIN_EMAIL` | Optional | Initial administrator email. Defaults to `admin@pri.local`. | `admin@example.test` |
| `SEED_ADMIN_PASSWORD` | **Required by Compose** | Initial administrator password. | `use-a-unique-secret` |
| `SEED_ADMIN_DISPLAY_NAME` | Optional | Initial administrator display name. Defaults to `Admin`. | `Recoup Administrator` |

### Backend application configuration

| Configuration key | Environment variable | Required? | Purpose |
|---|---|---:|---|
| `ConnectionStrings:Default` | `ConnectionStrings__Default` | Required unless `DATABASE_URL` is set | PostgreSQL connection string. |
| `DATABASE_URL` | `DATABASE_URL` | Alternative | PostgreSQL URI or connection string fallback. URI credentials must include database, username, and password; URI connections are configured with SSL required. |
| `Database:InitializeOnStartup` | `Database__InitializeOnStartup` | Optional; default `false` | When `true`, applies EF migrations and seeds the administrator and gateway catalog. Demo records are only seeded in Development and only when `Seed:DemoData=true`. |
| `Jwt:Key` | `Jwt__Key` | Required | JWT signing key; must be at least 32 characters. |
| `Jwt:Issuer` | `Jwt__Issuer` | Optional | Token issuer. Default: `PayPalRevenueIntelligence`. |
| `Jwt:Audience` | `Jwt__Audience` | Optional | Token audience. Default: `PayPalRevenueIntelligence.Client`. |
| `Jwt:ExpiresInMinutes` | `Jwt__ExpiresInMinutes` | Optional | JWT lifetime in minutes. Default: `60`. |
| `Seed:AdminEmail` | `Seed__AdminEmail` | Optional | Email for the initial administrator; blank means administrator seeding is skipped. |
| `Seed:AdminPassword` | `Seed__AdminPassword` | Optional | Initial administrator password; blank means administrator seeding is skipped. |
| `Seed:AdminDisplayName` | `Seed__AdminDisplayName` | Optional | Initial administrator display name. |
| `Seed:DemoData` | `Seed__DemoData` | Optional; default `false` | Allows backend fictional data seeding only when the host environment is `Development`. |
| `Cors:AllowedOrigins:0` | `Cors__AllowedOrigins__0` | Required for browser access from a non-default origin | Allowed frontend origin. Compose sets `http://localhost:5173`. Add further indexed entries for additional origins. |
| `PayPal:Environment` | `PayPal__Environment` | Optional; must be `sandbox` | The current code explicitly rejects any value other than Sandbox. |
| `PayPal:ClientId` | `PayPal__ClientId` | Optional | Server-side PayPal Sandbox client ID if configuring credentials outside the UI. |
| `PayPal:ClientSecret` | `PayPal__ClientSecret` | Optional | Server-side PayPal Sandbox client secret. |
| `PayPal:WebhookId` | `PayPal__WebhookId` | Optional | Webhook ID fallback when credentials are not saved through Settings. |
| `PayPal:CredentialStorePath` | `PayPal__CredentialStorePath` | Optional | File path for the encrypted PayPal credential store. Defaults to the current user's local application data directory. |
| `OpenAi:ApiKey` | `OpenAi__ApiKey` | Optional | OpenAI API key for chat and embedding requests. |
| `OpenAi:EmbeddingModel` | `OpenAi__EmbeddingModel` | Optional | Embedding model used by semantic cache. Default: `text-embedding-3-small`. |
| `OpenAi:EmbeddingDimensions` | `OpenAi__EmbeddingDimensions` | Optional | Embedding vector dimensions. Default: `256`; non-positive values omit the dimension parameter. |
| `OpenAi:EmbeddingPricePerMillionUsd` | `OpenAi__EmbeddingPricePerMillionUsd` | Optional | Configured USD price per million embedding tokens used for cost estimation. Default: `0.02`; verify against current provider pricing. |
| `Gemini:GeminiKey` | `Gemini__GeminiKey` | Optional | Google Gemini API key. |
| `Gateway:Cache:Enabled` | `Gateway__Cache__Enabled` | Optional | Semantic cache enablement. Default in `appsettings.json`: `true`. |
| `Gateway:Cache:SimilarityThreshold` | `Gateway__Cache__SimilarityThreshold` | Optional | Similarity threshold for cache lookup. Default: `0.93`. |
| `Gateway:Cache:TtlHours` | `Gateway__Cache__TtlHours` | Optional | Cache entry lifetime. Default: `24`. |
| `Gateway:Cache:MaxCandidates` | `Gateway__Cache__MaxCandidates` | Optional | Maximum cache candidates considered. Default: `500`. |
| `Gateway:Cache:MaxQueryCharacters` | `Gateway__Cache__MaxQueryCharacters` | Optional | Maximum query characters considered for semantic caching. Default: `8000`. |
| `DataProtection:KeyRingPath` | `DataProtection__KeyRingPath` | Recommended for persistent deployments | Directory used to persist ASP.NET Core Data Protection keys. Compose sets `/var/lib/recoup/dp-keys`. |
| `ASPNETCORE_ENVIRONMENT` | `ASPNETCORE_ENVIRONMENT` | Optional | ASP.NET Core environment. Compose sets `Development`. |
| `PORT` | `PORT` | Optional in container | Port consumed by the Dockerfile startup command; defaults to `10000`. Compose publishes container port 10000. |

The Compose file mounts `pri_api_data` at `/var/lib/recoup`, which persists the encrypted PayPal credential file and Data Protection key ring across container restarts. Keep this volume private and back it up securely.

## 6. Backend restore and run without Docker

From the repository root:

```bash
dotnet --info
dotnet restore backend/PayPalRevenueIntelligence.sln
dotnet build backend/PayPalRevenueIntelligence.sln
```

Set backend configuration in the shell or with .NET User Secrets. Example PowerShell session:

```powershell
$env:ConnectionStrings__Default = "Host=localhost;Port=5432;Database=pri;Username=pri;Password=REPLACE_ME"
$env:Jwt__Key = "REPLACE_WITH_AT_LEAST_32_RANDOM_CHARACTERS"
$env:Database__InitializeOnStartup = "true"
$env:Seed__AdminEmail = "admin@example.test"
$env:Seed__AdminPassword = "REPLACE_WITH_A_LONG_RANDOM_PASSWORD"
$env:Seed__AdminDisplayName = "Recoup Administrator"
$env:Cors__AllowedOrigins__0 = "http://localhost:5173"
```

Equivalent Bash example:

```bash
export ConnectionStrings__Default='Host=localhost;Port=5432;Database=pri;Username=pri;Password=REPLACE_ME'
export Jwt__Key='REPLACE_WITH_AT_LEAST_32_RANDOM_CHARACTERS'
export Database__InitializeOnStartup=true
export Seed__AdminEmail='admin@example.test'
export Seed__AdminPassword='REPLACE_WITH_A_LONG_RANDOM_PASSWORD'
export Seed__AdminDisplayName='Recoup Administrator'
export Cors__AllowedOrigins__0='http://localhost:5173'
```

Run the API:

```bash
dotnet run --project backend/src/PRI.Api/PRI.Api.csproj
```

The exact local HTTP/HTTPS ports are defined by `backend/src/PRI.Api/Properties/launchSettings.json`; inspect that file if you are not using Docker's `http://localhost:5000` mapping. Swagger UI is enabled only in the Development environment. The health endpoint is `/health`.

### Database migrations

Checked-in EF Core migrations are located in:

```text
backend/src/PRI.Infrastructure/Migrations/
```

The API startup path calls `InitializeDatabaseAsync()`. When `Database:InitializeOnStartup=true`, it applies `db.Database.MigrateAsync()`, seeds the first administrator if one does not already exist, and seeds the AI gateway client/model catalog and default routing rules. In Development, it additionally seeds fictional revenue data only if `Seed:DemoData=true`.

For manual migration application, install the EF Core CLI tool if needed and run:

```bash
dotnet tool install --global dotnet-ef --version 8.0.11
dotnet ef database update \
  --project backend/src/PRI.Infrastructure/PRI.Infrastructure.csproj \
  --startup-project backend/src/PRI.Api/PRI.Api.csproj
```

The repository includes EF Core design-time tooling in the API project. If your local tool installation reports a design-time startup/configuration error, use the startup project with the same database and JWT configuration required by the API.

### Administrator account

For Docker, the initial admin is seeded from `SEED_ADMIN_EMAIL`, `SEED_ADMIN_PASSWORD`, and `SEED_ADMIN_DISPLAY_NAME`. For local .NET execution, use `Seed__AdminEmail`, `Seed__AdminPassword`, and `Seed__AdminDisplayName`.

The admin is created only if the database does not already contain that email. Changing seed environment values after the account exists does not reset its password. Use the application's account-management flow or a deliberate administrative recovery procedure to change an existing account.

### Account registration and approval

The API supports public registration, but registration does **not** grant immediate access:

1. A visitor submits an email, display name, and password.
2. The API creates a pending account with the `member` role; the client cannot choose an administrator role.
3. An administrator reviews the account in user administration and activates it.
4. The member can log in after activation.

Passwords must contain at least 12 characters. Public registration is rate-limited to five requests per IP address per 15 minutes. The registration endpoint intentionally returns the same accepted response whether an email is new or already exists, and it does not send an email notification.

Administrators can alternatively create an active `admin` or `member` account directly. Admin user creation, activation, deactivation, and deletion are protected operations. The last active administrator cannot be disabled or deleted, and an administrator cannot disable or delete their own account.

For the Docker setup, use the administrator credentials configured through `SEED_ADMIN_EMAIL` and `SEED_ADMIN_PASSWORD` in the root `.env`. For local .NET execution, use `Seed__AdminEmail` and `Seed__AdminPassword`. The API contract also documents `admin@pri.local` / `Admin123!` as a local development demo account; do not assume that credential is active in a database initialized with custom seed settings.

## 7. PayPal Developer account and Sandbox setup

PayPal integration is optional. The backend uses the Sandbox base URL only; do not enter live credentials.

1. Create or sign in to a PayPal Developer account at the PayPal Developer Dashboard.
2. Open the Apps & Credentials area and switch to **Sandbox**.
3. Create a Sandbox REST app (or use an existing app).
4. Copy its Sandbox Client ID and Secret into a secure password manager or local secret store.
5. Confirm the Sandbox app has the API permissions needed for transaction search and, if using invoice features, PayPal Invoicing access.
6. Sign in to Recoup with an administrator account.
7. Open **Settings → PayPal connection** (the precise labels may vary with the UI language).
8. Save the Sandbox Client ID and Secret. The backend encrypts the saved credential payload using ASP.NET Core Data Protection.
9. Use **Test connection** to verify OAuth/API authentication.
10. Run a manual synchronization for a selected number of recent days and inspect the sync result.

### OAuth verification

The connection test obtains an OAuth access token from PayPal's Sandbox API using the client-credentials flow and performs the configured test request. A successful test confirms that the credentials can authenticate; it does not guarantee that every transaction or invoice endpoint is permitted for the app.

### Transaction synchronization

Use the PayPal connection controls in Settings to trigger a sync. The backend imports invoices where available, transaction-search results for the requested lookback period, and applicable refund updates. Transaction Search is requested in windows no longer than 31 days. Review the sync result for imported counts, skipped records, and the invoice-import-skipped flag.

If no transaction records appear, verify that the Sandbox account contains activity in the requested date range and that the app/account has the necessary API access.

### Optional webhook configuration

Webhook support is optional; manual sync works without it.

1. Configure a publicly reachable HTTPS endpoint for the backend.
2. In the PayPal Developer Dashboard, create a Sandbox webhook pointing to:
   `https://YOUR_PUBLIC_HOST/api/v1/webhooks/paypal`
3. Subscribe to the payment capture events supported by the implementation (completed, pending, denied/declined, refunded, reversed). The backend also accepts other event types but does not apply unsupported types immediately.
4. Copy the PayPal webhook ID.
5. Save the webhook ID with the PayPal connection credentials in Settings, or configure `PayPal__WebhookId` on the backend.
6. Deliver a Sandbox test event and inspect backend logs and stored payment state.

The webhook handler verifies PayPal signature headers and rejects missing/invalid signatures. It deduplicates event IDs. A webhook updates only a payment already imported into Recoup; it does not create unknown payment records. Run a subsequent sync to reconcile records and invoice state.

## 8. Configure OpenAI (optional)

Set the key on the backend only. With .NET User Secrets:

```bash
dotnet user-secrets init --project backend/src/PRI.Api
dotnet user-secrets set "OpenAi:ApiKey" "YOUR_OPENAI_API_KEY" --project backend/src/PRI.Api
```

Optional embedding settings:

```bash
dotnet user-secrets set "OpenAi:EmbeddingModel" "text-embedding-3-small" --project backend/src/PRI.Api
dotnet user-secrets set "OpenAi:EmbeddingDimensions" "256" --project backend/src/PRI.Api
dotnet user-secrets set "OpenAi:EmbeddingPricePerMillionUsd" "0.02" --project backend/src/PRI.Api
```

Or set environment variables:

```bash
OpenAi__ApiKey=YOUR_OPENAI_API_KEY
```

The shell syntax for setting environment variables differs by platform. Do not put a real key in a committed `appsettings*.json` file.

OpenAI is used for chat completions and semantic-cache embeddings. Real requests may incur provider charges. The configured embedding price is an application-side cost input; verify current OpenAI pricing and update the configuration when needed.

## 9. Configure Google Gemini (optional)

With .NET User Secrets:

```bash
dotnet user-secrets set "Gemini:GeminiKey" "YOUR_GEMINI_API_KEY" --project backend/src/PRI.Api
```

Or set the environment variable:

```bash
Gemini__GeminiKey=YOUR_GEMINI_API_KEY
```

Gemini is used for chat completion requests. The key remains on the backend. The seeded catalog includes a Gemini model entry; check current Google model availability and pricing before relying on catalog estimates.

## 10. AI model catalog, routing, caching, and budget

When database initialization is enabled, `GatewaySeeder` creates the first-party dashboard client, seeds the configured OpenAI and Gemini model catalog entries, and adds default routing rules if they are missing. Existing catalog/rule records are not overwritten by the seeder.

The initial routing rules cover default, classification, summarization, and analysis tasks. In the current seed data, default/classification/summarization prefer the economy OpenAI model with a standard fallback; analysis prefers the standard model with a premium fallback. Auto routing ranks eligible candidates using task fit and catalog pricing. The exact model identifiers and prices are stored in code/database seed data and may need updating as provider catalogs change.

In the AI Workspace, choose **Auto**, **OpenAI**, or **Gemini** when the UI offers provider selection. Auto can fall back to another eligible configured provider/model if a candidate is unavailable. Selecting a provider constrains the candidate pool. A provider-specific selection cannot work unless that provider is configured and its model is enabled.

The AI Cost/Settings area exposes budget and cache controls supported by the API. The budget is a cap: requests stop when the configured spend limit is reached. It is not used to select a cheaper model. Semantic caching requires pgvector support and an embedding provider/key. If embeddings are not configured or cache lookup fails, the gateway can proceed without a cache hit.

## 11. Frontend configuration

The frontend reads environment variables only in `frontend/src/config/env.ts`.

| Variable | Required? | Purpose | Default / example |
|---|---:|---|---|
| `VITE_API_BASE_URL` | Optional for mock-only demo; required for real API at a non-default address | Backend API base URL, including `/api/v1`. | `http://localhost:5000/api/v1` |
| `VITE_DATA_SOURCE` | Optional | Selects business-data adapter. `mock` uses fictional local sample data and disables login; `real` uses implemented backend operations. | Default is `mock` |
| `VITE_AI_SOURCE` | Optional | Selects workspace assistant adapter. `mock` uses predefined local responses; `real` sends requests to the backend AI gateway. | Default is `real` |

Create or edit `frontend/.env`:

```dotenv
VITE_API_BASE_URL=http://localhost:5000/api/v1
VITE_DATA_SOURCE=mock
VITE_AI_SOURCE=mock
```

Restart Vite after changing environment variables. Vite exposes `VITE_` variables to browser code; never put API keys, PayPal secrets, database passwords, or JWT signing keys in frontend environment variables.

### Real backend mode

To use the real business-data API and real AI gateway:

```dotenv
VITE_API_BASE_URL=http://localhost:5000/api/v1
VITE_DATA_SOURCE=real
VITE_AI_SOURCE=real
```

The API must be running, the frontend origin must be allowed by backend CORS, and the user must sign in. Real PayPal data additionally requires an administrator to configure and sync PayPal Sandbox. Real AI requests require at least one configured provider and can incur charges.

The real adapter maps implemented operations for revenue summaries, payment lists/details, recovery, cash flow, analytics, AI usage, budget/cache/model settings, and PayPal connection management. Review the current frontend adapter and API controllers before assuming every UI operation is backed by a live endpoint.

### Frontend dependencies and commands

```bash
cd frontend
npm ci
npm run dev
```

Open the URL printed by Vite (normally `http://localhost:5173`).

Available scripts:

| Command | Purpose |
|---|---|
| `npm run dev` | Start Vite development server. |
| `npm run build` | Run i18n validation, TypeScript project build, and production Vite build. |
| `npm run check:i18n` | Validate translation keys with `scripts/check-i18n.mjs`. |
| `npm run typecheck` | Run TypeScript project checks. |
| `npm run check` | Run i18n validation and type checking. |
| `npm run preview` | Preview the production build locally. |

## 12. Run the application

### Local frontend demo (no backend or PayPal required)

For a fully local presentation with fictional business data and predefined AI responses:

```dotenv
VITE_DATA_SOURCE=mock
VITE_AI_SOURCE=mock
```

Then:

```bash
cd frontend
npm ci
npm run dev
```

This mode does not require PostgreSQL, PayPal credentials, or AI provider keys. The dashboard and interactions use the frontend's bundled mock adapter. Do not describe these records as live PayPal data.

### Demo business data with real AI (optional)

The default frontend configuration selects mock business data and real AI. To use that combination:

```dotenv
VITE_DATA_SOURCE=mock
VITE_AI_SOURCE=real
VITE_API_BASE_URL=http://localhost:5000/api/v1
```

Start the backend and configure at least one AI provider. Sign in to use the real AI gateway. The business records remain fictional, while AI requests are real provider calls and may incur cost.

### Full backend and PayPal Sandbox mode

1. Start PostgreSQL and the backend using Docker Compose or local .NET.
2. Ensure the API is reachable at the configured `VITE_API_BASE_URL`.
3. Set `VITE_DATA_SOURCE=real` and `VITE_AI_SOURCE=real`.
4. Start the frontend.
5. Sign in with the seeded administrator account.
6. Configure PayPal Sandbox credentials in Settings, test the connection, and run a sync.
7. Configure OpenAI and/or Gemini on the backend if you want real AI functionality.

### Fully offline AI demo

For a demo with no AI provider calls, use:

```dotenv
VITE_DATA_SOURCE=mock
VITE_AI_SOURCE=mock
```

The assistant returns predefined local responses. This is the only mode in which the AI Workspace does not require the backend or external AI provider credentials.

## 13. Production and deployment considerations

The included Compose file is a local development stack. Before any public or production deployment:

- Do not run with default or shared secrets. Generate unique database, JWT, admin, PayPal, and AI provider credentials.
- Set `ASPNETCORE_ENVIRONMENT=Production` and configure `Database:InitializeOnStartup` deliberately. The supplied Compose configuration uses Development and startup initialization for local convenience.
- Configure the exact frontend origin in `Cors:AllowedOrigins`; do not use wildcard origins with credentialed access.
- Terminate TLS at a trusted ingress/reverse proxy and expose only required ports.
- Persist and protect PostgreSQL data, `/var/lib/recoup` (encrypted PayPal credential file and Data Protection key ring), and backup encryption keys. Losing the Data Protection key ring can make saved PayPal credentials unreadable.
- Use a managed PostgreSQL instance with pgvector support, backups, restore tests, connection limits, and monitoring.
- Keep secrets in the deployment platform's secret manager or environment configuration, not in images or repository files.
- Restrict administrator access and review account activation/deactivation procedures.
- Configure webhook URL, webhook ID, HTTPS, and PayPal Sandbox/live policy intentionally. This version supports Sandbox only.
- Review provider model IDs and pricing catalog values regularly; compare estimated AI cost with actual provider invoices.
- Add centralized logs, metrics, tracing, alerting, and durable job processing before relying on the in-process recovery worker for business-critical operations.
- Run the backend tests and frontend checks/build in CI before deployment.

The repository includes a backend Dockerfile for the API and a Compose file for local PostgreSQL/API startup. It does not include a complete production deployment manifest, CI pipeline, or infrastructure-as-code definition in the inspected snapshot.

## 14. Troubleshooting

| Symptom | Likely cause and action |
|---|---|
| Compose refuses to start because `POSTGRES_PASSWORD` is missing | Add `POSTGRES_PASSWORD` to the root `.env` file. |
| API exits with a JWT key configuration error | Set `JWT_KEY` (Compose) or `Jwt__Key` (local run) to a random value at least 32 characters long. |
| API cannot connect to PostgreSQL | Check `ConnectionStrings__Default`/`DATABASE_URL`, database health, credentials, and whether the host/port is correct. Inside Compose, the API connects to host `postgres`, not `localhost`. |
| Database initialization does not run | Set `Database__InitializeOnStartup=true`. Confirm the API is using the intended environment and database. |
| Login fails after changing seed credentials | Seeding only creates an admin if the email is absent; it does not reset an existing user's password. |
| Browser reports CORS errors | Add the exact frontend origin (including scheme and port) to `Cors__AllowedOrigins__0` and restart the backend. |
| Frontend cannot reach the API | Verify `VITE_API_BASE_URL`, API port, API health endpoint, and that Vite was restarted after `.env` changes. |
| Real-data pages show authorization errors | Set `VITE_DATA_SOURCE=real`, sign in, and confirm the account is active. Admin-only settings and sync require an administrator role. |
| PayPal test connection fails | Check Sandbox (not Live) credentials, app status, network access, and server logs. A successful OAuth test does not prove invoice permissions. |
| Transactions sync but invoices do not | The Sandbox app/account may lack Invoicing access. Check the sync result's invoice-import-skipped status and PayPal app permissions. |
| No transactions are imported | Confirm Sandbox account activity exists in the requested lookback window and transaction-search access is enabled. |
| Webhook requests are rejected | Confirm the public HTTPS URL, webhook ID, raw PayPal signature headers, and that the event was sent from the matching Sandbox app. |
| Webhook event does not create a payment | This is expected for an unknown payment. The event handler updates only an already-synchronized payment; run sync. |
| OpenAI/Gemini calls fail | Confirm backend-only key configuration, provider account status, model availability, network access, and remaining provider quota/budget. |
| AI Cost estimates differ from provider billing | The application uses stored catalog prices and reported token usage; provider billing tiers and price changes can differ. |
| Semantic cache is unavailable | Confirm PostgreSQL/pgvector support, cache configuration, and OpenAI embedding configuration. The gateway may continue without a cache hit. |
| Saved PayPal credentials cannot be decrypted after restart | Ensure the same Data Protection key ring is persisted and mounted at `DataProtection:KeyRingPath`. |
| Frontend build fails | Run `npm ci`, then `npm run check` and `npm run build`; use a Node.js release compatible with Vite 8. |

## 15. Security notes

- Never commit `.env`, provider keys, PayPal secrets, JWT keys, passwords, or credential exports.
- Use separate secrets for database authentication, JWT signing, administrator login, PayPal, and AI providers.
- Treat any credential-shaped value that has ever been committed or shared as compromised. Revoke/rotate it rather than merely deleting it from the latest file.
- Keep PayPal and AI provider keys on the backend. `VITE_` variables are public browser configuration.
- Use a strong JWT key (at least 32 characters) and protect it as a secret.
- Keep the encrypted PayPal credential file and Data Protection key ring private, persistent, and backed up securely.
- Use HTTPS for deployed frontend/API traffic and PayPal webhooks.
- Limit administrator accounts and review user approval/activation.
- Do not expose Swagger publicly without an intentional access policy.
- Do not use the fictional demo dataset for financial decisions or claim that demo recommendations executed external actions.
- Review provider usage and budget controls; real AI requests can incur charges.
- The current implementation is Sandbox-only and is not a production payment-processing authorization.

## 16. Verification checklist

After setup, verify the relevant mode:

- [ ] `docker compose -f local-docker-compose.yml ps` shows the database and API running (if using Compose).
- [ ] `http://localhost:5000/health` responds (adjust the port if changed).
- [ ] Swagger UI is available in Development at `/swagger`.
- [ ] The seeded administrator can sign in.
- [ ] The frontend opens at `http://localhost:5173`.
- [ ] Mock mode displays clearly fictional sample records.
- [ ] Real mode can authenticate and load implemented backend operations.
- [ ] PayPal Sandbox connection test succeeds before attempting sync.
- [ ] Sync results are reviewed for skipped records and invoice-import limitations.
- [ ] AI provider configuration is tested before enabling real AI in a demonstration.
- [ ] The fully offline demo uses `VITE_AI_SOURCE=mock`.

For product scope and the judge-facing walkthrough, see [README.md](./README.md).
