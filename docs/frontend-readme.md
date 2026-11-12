# Recoup - PayPal Revenue Intelligence: Frontend

The web interface of Recoup. It shows PayPal payments and lost/recovered revenue, lets the user approve or reject AI-suggested recovery actions, tracks what the AI costs, and offers an AI chat. The backend (ASP.NET Core) lives in `backend/`; this document covers only `frontend/`.

Product rules the UI follows everywhere:
- The AI only proposes. Nothing external happens before the user approves.
- The UI never does financial arithmetic. Amounts, rates and savings come from the backend and are only formatted.
- Sample data is always labelled with a "Mock data" badge.
- No user-visible text lives in components. All text is in `src/i18n/locales`.

## Run it

```
cd frontend
npm install
cp .env.example .env      # on Windows PowerShell: copy .env.example .env
npm run dev               # http://localhost:5173
```

Node 22.22 or newer (or 24 LTS) is recommended; older 22.x versions only print warnings.

| Command | What it does |
|---|---|
| `npm run dev` | Development server |
| `npm run build` | i18n check, type check, production build into `dist/` |
| `npm run preview` | Serves the production build locally |
| `npm run check` | i18n check and type check without building |

## Settings (`frontend/.env`)

| Variable | Meaning |
|---|---|
| `VITE_API_BASE_URL` | Backend address, for example `http://localhost:5000/api/v1`. The only place an API address may appear. |
| `VITE_DATA_SOURCE` | `mock` (default): business data comes from the fictional local dataset. `real`: data operations use the backend. |
| `VITE_AI_SOURCE` | `real` (default): AI Workspace calls the authenticated backend and requires login, even with mock business data. `mock`: predefined local assistant responses; no AI provider call. |

## Pages

| Address | Page | Purpose |
|---|---|---|
| `/` | Overview | Key figures, revenue trend, recovery and AI usage summary, recent failed payments |
| `/payments` | Payments | Searchable, filterable, sortable payment list with a detail drawer and status history |
| `/recovery` | Recovery | Lost/recovered revenue and AI suggestions to approve or reject |
| `/ai-cost` | AI Cost | AI spend, tokens, cache hit rate, savings, budget, cost by model/provider |
| `/ai-workspace` | AI Workspace | Streaming AI chat with a cost receipt per answer |
| `/settings` | Settings | Language, dark mode, AI budget, response cache, model catalog |
| `/login` | Login | Sign in (real mode only) |
| `/design-system/*` | Developer pages | Component gallery, design tokens, data inspector with a "simulate backend failure" switch |

A page-by-page description of what every filter and button does is in the site guide (`PayPal-Revenue-Intelligence-Site-Rehberi.txt`).

## Folder layout (`frontend/src`)

| Folder | Content |
|---|---|
| `app/` | App shell wiring: providers, routes (lazy-loaded), theme, error boundary |
| `pages/` | One file per screen |
| `features/` | Data hooks and feature-specific pieces (revenue, recovery, ai-cost, workspace, settings, auth) |
| `components/ui/` | Basic building blocks (Button, Card, Dialog, Select, Switch, Toast...) |
| `components/patterns/` | Reusable compositions (DataTable, FilterBar, StatCard, TrendChart, ApprovalBanner, MoneyText, StatusPill, EmptyState, MockDataBadge) |
| `components/layout/` | Sidebar, top bar, app shell, navigation list |
| `services/` | The API layer: types, HTTP client, auth, adapters |
| `i18n/` | Translations (EN, DE, TR, ES), formatting helpers |
| `styles/` | Design tokens (colours, radius, shadows), light and dark theme |

## The API layer (`src/services`)

This is the part that changes when the backend arrives.

- `types/` holds the data shapes. They follow `docs/API_CONTRACT.md` (money is `{ amount: "decimal string", currency }`, pagination is `{ items, page, pageSize, totalItems }`, enums are snake_case, timestamps are UTC with `Z`).
- `adapters/types.ts` defines the `Api` interface: the full list of operations the UI uses, grouped as `revenue`, `aiCost`, `settings`, `workspace`.
- `adapters/mock/` is the sample-data implementation. `adapters/real/` calls the backend.
- `adapters/index.ts` composes the selected business-data adapter with the independently selected AI Workspace adapter. In mock-data/live-AI mode, a bounded fictional-data snapshot is passed as context; it is not sent to payment lookup endpoints.
- `http.ts` sends every request with the JWT and the `Accept-Language` header (en/de/tr/es, so AI text comes back in the user's language), turns errors (RFC 9457, with a `code`) into readable messages, and on 401 signs the user out.

### Connecting an endpoint to the real backend

1. In `docs/API_CONTRACT.md`, check that the endpoint is marked Ready.
2. Open `src/services/adapters/index.ts` and add the operation name to `READY`. Names are `group.operation`:

   | Group | Operations |
   |---|---|
   | `revenue` | `getSummary`, `listPayments`, `getPayment`, `getRecoverySummary`, `listSuggestions`, `decideSuggestion`, `generateSuggestions` |
   | `aiCost` | `getUsageSummary`, `getUsageSeries`, `getBreakdown` |
   | `settings` | `getBudget`, `setBudget`, `listModels`, `getCacheStats`, `clearCache` |
   | `workspace` | `streamChat` |

3. Set `VITE_DATA_SOURCE=real` and `VITE_AI_SOURCE=real`, start the backend, sign in at `/login`, and check the screen. To use live AI with fictional business data instead, keep `VITE_DATA_SOURCE=mock` and set `VITE_AI_SOURCE=real`.
4. If the response differs from the contract, fix it in `adapters/real/index.ts` or `types/` (or report it to the backend). Do not change the screens.

The "Mock data" badge disappears by itself once no operation is left on mock.

## Where the data is still mock

- All data operations stay on mock until the contract marks them Ready (today only health, ping and auth are live).
- With `VITE_DATA_SOURCE=mock`, the AI Cost page remains fictional even when AI Workspace calls the live backend; only the per-answer receipt reflects the real completion.
- Settings: request and response shapes are proposed (`types/settings.ts`), because the contract lists the endpoints without schemas.
- Routing rules are intentionally not shown. Their schema is undefined, and the UI must not show a setting the backend cannot store.

## Rules for new code

- Texts: add the key to `i18n/locales/en.ts` first. DE, TR and ES are typed against it, so a missing key fails the build. `npm run check:i18n` rejects hard-coded UI text.
- Money: show it with `MoneyText`; never add, multiply or convert amounts in the UI.
- Colours and spacing: use the tokens in `styles/`, so light and dark themes keep working.
- Error and empty states: every data block needs loading, error (with retry) and empty handling.
- Responsive: check 320 px, 390 px and desktop, in light and dark.
- AI actions: anything the AI proposes that reaches outside the app must go through the approval flow (`features/recovery/useSuggestionActions.tsx`).

## Quality checks done

Type check, i18n check and production build pass. Every page was checked at 320, 390 and 768 px in light and dark and in all four languages (no horizontal overflow, no console errors). Error states and the crash screen were tested. Real mode was tested with a login stub, not yet against the real backend.
