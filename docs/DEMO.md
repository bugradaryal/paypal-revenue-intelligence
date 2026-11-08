# Demo guide

## Start
```
cd frontend
npm install
cp .env.example .env     # VITE_DATA_SOURCE=mock is the default
npm run dev
```
Mock mode needs no backend and no login. A "Mock data" badge is always visible in the top bar.

## Suggested 5-minute walkthrough
1. **Overview** – revenue, failed and pending payments, recovered revenue, trend chart (use "Table view" for the accessible version), recovery and AI usage cards. Switch 7 / 30 / 90 days.
2. **Payments** – filter by status, search, sort, paginate. Click a row for the detail drawer with status history. Filters live in the URL, so the view is shareable.
3. **Recovery** – the banner says nothing happens without approval. Approve an executable suggestion: a confirmation dialog appears, then the status moves to "executed" after a few seconds (the page polls). "Generate suggestions" is user-triggered only.
4. **AI Cost** – spend, requests, savings, budget bar, per-model and per-provider tables. Every figure comes from the backend; the UI does no money arithmetic.
5. **AI Workspace** – click a starter question. The answer streams, ends with a cost receipt, and may propose an action (same approval rule). Ask the same question again to see a free cache hit. Try Stop and "Answer again".
6. **Settings** – change language (EN / DE / TR / ES), dark mode, the AI budget (watch AI Cost update) and clear the cache.
7. **Error states** – open `/design-system/data`, turn on "Simulate backend failure", then navigate: every card shows its own error with "Try again".

## Real backend
Set in `frontend/.env`:
```
VITE_DATA_SOURCE=real
VITE_API_BASE_URL=http://localhost:5000
```
Sign in at `/login` (demo user in `API_CONTRACT.md`). Only operations the contract marks Ready go to the backend (`READY` in `src/services/adapters/index.ts`); the rest stay on the mock adapter, and the badge stays visible while any do.
To switch an operation over, add its key to `READY` once the contract marks it Ready.

## Known limits
- AI Workspace streaming is mocked until contract 0.4 defines the SSE schema.
- Settings request/response shapes are proposed; routing rules are not shown (no schema).
- USD only, as in the contract's MVP.
