# Transactions Ingest – UI

Angular 20 (standalone components, signals, zoneless) + Bootstrap 5 + Tailwind 4 (`tw:` prefix, no preflight so it doesn't fight Bootstrap).

```bash
npm install
npm start        # http://localhost:4200
npm run build    # dist/transactions-ingest-ui/browser
```

## API url

Set per environment in `src/environments/`:

| File | Used by | `apiBaseUrl` |
|---|---|---|
| `environment.development.ts` | `ng serve` | `https://localhost:44380/api` (API in IIS Express/IIS, CORS allowed for `localhost:4200`) |
| `environment.ts` | `ng build` (production) | `/api` (UI served from the API's wwwroot, same origin) |

If the API runs on another port, change `environment.development.ts` and add the UI origin to `Cors:AllowedOrigins` in the API's `appsettings.json`.

## Structure

```
src/app/
  core/                       app-wide singletons, used once
    http/                     interceptors (ProblemDetails -> toast)
    layout/                   header, toast container
    services/                 notification service
  shared/                     reusable, feature-agnostic
    components/pagination/
    models/                   PagedResult, SortDirection
    utils/                    toHttpParams
  features/
    transactions/
      data-access/            api services (http only, no ui state)
      models/                 api contracts for this feature
      components/             presentational: filters, table, status badge, summary, audit history
      pages/                  routed containers: list page, detail page
      transactions.routes.ts  lazy loaded from app.routes.ts
```

Rules I followed: pages own the state and talk to data-access, components only get inputs and emit outputs.
`shared` never imports from `features`. Path aliases: `@core/*`, `@shared/*`, `@features/*`, `@env/*`.
