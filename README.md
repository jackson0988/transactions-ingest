# Transactions Ingest

Imports card transactions from three source systems (Store Uptown, Store Downtown, Online), reconciles them into SQLite
(insert / update / revoke / finalize) with a full audit trail, and shows them in an Angular UI.

## Repository layout

```
transactions-ingest/
├── backend/                                  ASP.NET 10 Web API
│   ├── TransactionsIngest.slnx
│   ├── Directory.Build.props                 shared build settings
│   ├── Directory.Packages.props              central NuGet versions
│   ├── global.json
│   ├── src/
│   │   ├── TransactionsIngest.Domain/        entities, enums, constants - no dependencies
│   │   ├── TransactionsIngest.Application/   use cases: reconciliation rules, ports (ITransactionSource...), DTOs
│   │   ├── TransactionsIngest.Infrastructure/ EF Core + SQLite, import orchestration, mock sources, queries
│   │   └── TransactionsIngest.Api/           controllers, error handling, config, sample data
│   └── tests/
│       ├── TransactionsIngest.UnitTests/         reconciler + validator (no db)
│       └── TransactionsIngest.IntegrationTests/  import on in-memory sqlite, API via WebApplicationFactory
├── frontend/                                 Angular 20 UI (see frontend/README.md)
└── docs/
    ├── architecture.md                       design decisions, rules, assumptions
    └── backend-guide.md                      end-to-end walkthrough: every route, import flow, data model
```

Dependencies point inwards only: `Api -> Infrastructure -> Application -> Domain`.

## Prerequisites

- .NET 10 SDK
- Node 20.19+ / 22.12+

## Build & run

Backend:

```bash
cd backend
dotnet build
dotnet test
dotnet run --project src/TransactionsIngest.Api --launch-profile https
```

API runs on `https://localhost:7180`. The OpenAPI doc is at `/openapi/v1.json` in Development, and requests to try are in `TransactionsIngest.Api.http`.

Frontend, in a second terminal:

```bash
cd frontend
npm install
npm start
```

Open http://localhost:4200 and click **Run import**.

### Hosting the API in IIS (https)

**IIS Express** (Visual Studio): open `backend/TransactionsIngest.slnx`, pick the **IIS Express** profile and run.
The API is then on `https://localhost:44380`, which is what `frontend/src/environments/environment.development.ts` points to.

**Full IIS:**

1. Install the [ASP.NET Core 10 Hosting Bundle](https://dotnet.microsoft.com/download/dotnet/10.0), then run `iisreset`.
2. Publish the API. This also creates the `web.config`:
   ```bash
   cd backend
   dotnet publish src/TransactionsIngest.Api -c Release -o C:\inetpub\transactions-ingest-api
   ```
3. In IIS Manager, create an app pool with **.NET CLR version = No Managed Code**. Then add a site pointing at that folder,
   with an **https** binding and a trusted certificate (e.g. the "IIS Express Development Certificate").
4. Give the app pool identity (`IIS AppPool\<pool name>`) **Modify** rights on the folder, because SQLite writes `transactions.db` there.
5. Optional: set `ASPNETCORE_ENVIRONMENT=Development` on the site (Configuration Editor -> `system.webServer/aspNetCore` -> environmentVariables)
   if you want the OpenAPI endpoint.
6. Put the site's https url into `apiBaseUrl` in `environment.development.ts`.

The UI origin (`http://localhost:4200`) must be listed in `Cors:AllowedOrigins` in `appsettings.json`.

### Database / migrations

The SQLite file `transactions.db` is created in the API project folder on first start. As long as no migrations exist,
startup uses `EnsureCreated`. To switch to migrations:

```bash
cd backend
dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialCreate -p src/TransactionsIngest.Infrastructure -s src/TransactionsIngest.Api -o Persistence/Migrations
```

Delete the old `transactions.db` once after that.

## API

| Method | Route | |
|---|---|---|
| POST | `/api/import` | Runs an import of all enabled sources. Returns 409 if one is already running. |
| GET | `/api/transactions` | Paged list. Query params: `page`, `pageSize` (max 200), `sourceSystem`, `status`, `search`, `sortBy`, `sortDir` |
| GET | `/api/transactions/{id}` | Single transaction |
| GET | `/api/transactions/{id}/audit` | Audit history, newest first |
| GET | `/api/import/batches` | Last import runs |

Errors are returned as RFC 7807 ProblemDetails.

## Trying the reconciliation

The mock sources read `backend/src/TransactionsIngest.Api/SampleData/*.json` on every import. While the app is running:

- change an amount or product name and import again: the transaction is updated and each changed field gets its own audit entry
- delete a row and import again: the transaction is revoked
- set `"SimulateFailure": true` for a source in `appsettings.json` and restart: that source fails and nothing of it gets revoked

More detail in [docs/architecture.md](docs/architecture.md).
