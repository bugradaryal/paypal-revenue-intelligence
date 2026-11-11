# Recoup - PayPal Revenue Intelligence

AI-powered revenue & cost optimization platform.

```
paypal-revenue-intelligence/
├── backend/    ASP.NET Core Web API (separate workspace)
├── frontend/   React + TypeScript + Vite + Tailwind CSS v4
├── docs/       API_CONTRACT.md (owned by the backend side)
└── README.md
```

## Frontend quick start

```bash
cd frontend
cp .env.example .env     # on Windows: copy .env.example .env
npm install
npm run dev              # http://localhost:5173
```

| Script | Purpose |
|---|---|
| `npm run dev` | Dev server on :5173 |
| `npm run build` | Typecheck + production build |
| `npm run typecheck` | TypeScript only |

The API address lives only in `VITE_API_BASE_URL` (see `frontend/.env.example`).
`VITE_DATA_SOURCE=mock` uses the local mock adapter until the API contract is settled.

## Frontend structure (as of Phase 3)

| Path | Purpose |
|---|---|
| `src/styles/index.css` | Design tokens (light/dark), Tailwind v4 `@theme` |
| `src/components/ui/` | Primitives (Button, Input, Dialog, Table, ...) |
| `src/components/layout/` | AppShell, Sidebar, Topbar, navigation config |
| `src/components/patterns/` | Composed, app-aware blocks (MockDataBadge, ...) |
| `src/i18n/` | EN / DE / TR / ES. English is the source; other locales are type-checked against it |
| `src/app/` | Providers (theme, i18n, query client, toasts) and routes |
| `src/services/` | `http.ts` (single fetch client), `errors.ts`, `types/` (proposed DTOs), `adapters/` (`mock/` and `real/` behind one `Api` interface) |
| `src/features/*/queries.ts` | TanStack Query hooks. Pages use these, never the adapters directly |
| `src/lib/` | `cn`, dates, display-only formatters (money/number/percent/date) |

Routes: `/`, `/payments`, `/recovery`, `/ai-cost`, `/ai-workspace`, `/settings`.
Internal pages: `/design-system` (components), `/design-system/tokens`.

To add a language: create `src/i18n/locales/<code>.ts` typed as `Messages`, then register it in `src/i18n/index.tsx`.

## Data layer (Phase 4)

`VITE_DATA_SOURCE=mock|real` picks the adapter. While `mock` is active the UI shows a **Mock data** badge, and all
mock data is fictional. The `real` adapter throws `ContractNotReadyError` for every operation until it is defined
in `docs/API_CONTRACT.md`; the frontend never guesses endpoints. What the frontend needs from the backend is listed
in `docs/API_NEEDS.md`.

Check the data flow at `/design-system/data` (loading, error, approval flow, simulated backend failure).

## Internationalization (update after Phase 4)

Every visible string goes through i18n (EN, DE, TR, ES). No hardcoded UI text, no English-only pages.

- Add new keys to `src/i18n/locales/en.ts` first; `de.ts`, `tr.ts` and `es.ts` are typed against it, so a missing key fails the build.
- `npm run check:i18n` scans `src/**/*.tsx` for hardcoded text (JSX text, `aria-label`, `placeholder`, `title`, ...) and runs as part of `npm run build`. Use `{/* i18n-ignore */}` or a trailing `// i18n-ignore` only for real code identifiers.
- Values from the backend (statuses, failure codes, error codes) are translated with `tryT(key, fallback)`, so an unknown value shows the fallback instead of a raw key.
- Text written by the backend or the AI (e.g. suggestion rationales) is requested in the active language via the `Accept-Language` header; queries refetch when the language changes. The mock adapter imitates this.

## Contract alignment (API_CONTRACT.md v0.4)

- `docs/API_CONTRACT.md` is the single source of truth and is owned by the backend. `src/services/types/` mirrors it.
- `VITE_DATA_SOURCE=real` uses the backend only for operations listed in `READY` in `src/services/adapters/index.ts`; every other operation stays on the mock adapter, as the contract prescribes. Move an operation into `READY` when the contract lists its endpoint as Ready. `src/services/adapters/real/` already contains all calls, written from the contract.
- Auth is real in `real` mode: `/login`, JWT in `sessionStorage`, a 401 on an authenticated request signs the user out. Mock mode has no login.
- Every request sends `Accept-Language`. AI generation calls use a 60 s timeout.
- Dev server port 5173 is the CORS origin the backend allows.

## Pattern components (Phase 5)

`src/components/patterns/`: StatCard, TrendChart, DataTable, FilterBar (SearchField, SegmentedControl, ChipFilter), StatusPill, MoneyText, EmptyState.

- Components format only. Money always goes through `MoneyText`; nothing in the UI calculates financial values.
- Chart colours are tokens (`--chart-1` revenue orange, `--chart-2` recovered green), validated with the data-viz palette checker for both themes. Charts have a legend, a hover/keyboard tooltip and a table view.
- Chart days are UTC days and are shown as UTC (`useFormat().day`), so a bucket never shifts to another date.
- Every record with `dataSource: "demo"` shows a "Demo" badge.
- Overview (`/`) uses revenue summary, recovery summary, failed payments and AI usage summary.

## Payments (Phase 6)

`/payments`: search, status chips, time range, sortable columns (amount, date), pagination (20 per page) and a detail drawer with the status history. Filters, sort, page and the open payment are kept in the URL, so reload and shared links show the same view. Uses `GET /revenue/payments` and `GET /revenue/payments/{id}` through the adapter (mock until the contract marks them Ready).

## Recovery (Phase 7)

`/recovery`: recovery summary (lost, recovered, rate, open suggestions), an `ApprovalBanner`, and the AI suggestion list (status chips, sorting, pagination).

- The AI only proposes. Approve / reject is the user's decision; an executable suggestion (PayPal reminder) asks for an explicit confirmation first. Non-executable ones say "advice only: nothing runs automatically".
- After approving an executable suggestion the list re-queries every 2.5 s for at most 30 s until the status becomes `executed` or `failed` (contract section 5).
- "Generate suggestions" calls `POST /revenue/suggestions/generate` (60 s timeout, never automatic) and sends `Accept-Language`, so the AI text is written in the UI language. A suggestion written in another language says so.
- A 409 `suggestion_already_decided` shows a message and refreshes the list.
- `ApprovalBanner` and `SuggestionCard` are meant to be reused by the AI Workspace (Phase 9).

## AI Cost (Phase 8)

`/ai-cost`: total cost, tokens (input / output), cache hit rate, savings, the monthly budget, cost and request charts per day, and a breakdown by model or provider.

## PayPal connection

In real data mode, **Settings** shows the configured PayPal Sandbox connection and last synchronization result.
Admin users can test OAuth authentication and import the previous 30 days of transactions; a successful sync
refreshes revenue data. Enter and save Sandbox credentials directly in the Settings page; the API stores them
encrypted in its local application data and never returns the secret. Saving new credentials replaces the old
ones. The current version supports one Sandbox business account only; Live and multi-account OAuth are not
supported. For a multi-instance deployment, configure a shared protected credential-store path and persist the
ASP.NET Core Data Protection key ring securely across restarts.

## Switching between mock and backend demo data

There are two separate ways to use sample data:

### Frontend mock mode

In `frontend/.env`, set:

```env
VITE_DATA_SOURCE=mock
```

Restart the Vite dev server after changing the value. The frontend then uses its local fictional mock adapter for
all supported screens; it does not fetch revenue data from the backend, and login is disabled.

### Backend demo records with the real frontend

To keep the frontend connected to the API while displaying the backend's clearly labeled demo records:

1. Keep `VITE_DATA_SOURCE=real` in `frontend/.env`.
2. In `backend/src/PRI.Api/appsettings.Development.json`, set `Seed.DemoData` to `true`.
3. Restart the backend. On startup, it inserts demo records if they are not already present and includes demo records
   in revenue and recovery queries.
4. Restart the frontend if its `.env` value changed.

Set `Seed.DemoData` back to `false` and restart the backend to exclude demo records from those queries; existing demo
records are not deleted. This setting is intended for local development only, not production.

- Everything shown is what the backend reports. Savings are measured by the gateway against a baseline model (contract section 5); the UI never estimates them. The provider list is never assumed: rows come from the response.
- AI costs show up to four decimals. The budget's `spent` is the current UTC month, independent of the selected range, and the page says so. The bar under it is only a visual scale of the two reported amounts.
- `TrendChart` now accepts counts as well as money, and hides its legend for a single series.


## AI Workspace and Settings

**AI Workspace** (`/ai-workspace`) is a streaming chat. The client keeps the conversation and sends the history with every request (the contract has no server-side conversations). Each answer shows its model, tokens and cost, and a cache hit is labelled free. If the assistant proposes an action, it appears as the same suggestion card used on the Recovery page, with the same approval rule and confirmation dialog. Nothing external happens until the user approves.

**Settings** (`/settings`) holds language and dark mode (stored in this browser), the AI budget, the response cache (entry count and clear) and a read-only model catalog.

Status against the contract:
- The workspace stream runs on a **mock** assistant until contract 0.5 defines the SSE schema. The real adapter throws `ContractNotReadyError` for it.
- The settings request and response shapes (`types/settings.ts`) are **proposed**, because the contract lists the endpoints but not the schemas. They stay on the mock adapter until the contract marks them Ready.
- **Routing rules are deliberately not shown.** Their schema is undefined, and the UI must not show settings the backend cannot store.

## Phase 10: polish
- Every page is lazy-loaded; React and TanStack Query are split into their own chunks (largest chunk about 256 kB, no size warning).
- `ErrorBoundary` wraps the app and each route, so a render error or a failed chunk shows a retry card instead of a blank screen. It resets on navigation.
- Failed queries no longer show stale numbers next to the error message.
- Checked at 320 / 390 / 768 px, light and dark, in EN / DE / TR / ES on every page: no horizontal overflow, no console errors.
- Real mode verified against a login stub: redirect to `/login`, sign-in, mock fallback for non-Ready operations with the badge shown.
- See `docs/DEMO.md` for the walkthrough and the switch to the real backend.
