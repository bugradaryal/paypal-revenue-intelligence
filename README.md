# Recoup — PayPal Revenue Intelligence

**Recoup helps teams understand where PayPal revenue is slipping away and what their AI workloads cost.**
It brings payment performance, recovery opportunities, and AI usage economics into one workspace—so teams can
move from scattered signals to informed decisions.

## The problem

Businesses can lose revenue when payments fail, while the information needed to spot patterns and follow up is
spread across separate tools. At the same time, AI features introduce a second cost challenge: choosing models
without understanding the trade-off between price, capability, and usage.

Recoup is designed to make both sides visible together. It does not claim that every failed payment can be
recovered or that AI costs can be reduced by a fixed amount. Instead, it gives operators the context to decide
what to investigate and what action to take.

## The approach

Recoup turns operational data into a human-reviewed workflow:

1. **Observe** revenue, payment status, failed transactions, and AI usage in one dashboard.
2. **Investigate** payment history, failure reasons, recovery performance, and cost drivers.
3. **Prioritize** recovery opportunities with AI-assisted suggestions.
4. **Review and approve** any customer-facing action. The AI proposes; a person decides.
5. **Optimize** AI workloads with model routing, response caching, and cost tracking.

This keeps automation useful without removing human oversight.

## What the product includes

- **Revenue overview:** revenue, failed and pending payments, recovered revenue, and daily trends.
- **Payment investigation:** searchable and filterable payment records with status history and failure details.
- **Recovery workspace:** AI-assisted recommendations, expected recovery values, and explicit approve/reject
  controls. Executable reminders require confirmation before they are sent.
- **AI cost intelligence:** token usage, provider/model breakdowns, cache hit rate, daily cost trends, and a
  configurable monthly budget.
- **AI workspace:** a conversational interface for exploring payment and cost questions, with model and cost
  details shown alongside each answer.
- **PayPal connection management:** Sandbox credentials, OAuth connection checks, manual transaction sync, and
  optional webhook notifications. If the Sandbox app cannot access Invoicing, invoice import is skipped with a
  warning while transaction synchronization continues.
- **Admin tools:** user and role management for controlled access.

## A product walkthrough

For a concise demonstration:

1. Open **Overview** to introduce the revenue picture and recent trends.
2. Go to **Payments** and inspect a failed payment, its reason, and status history.
3. Open **Recovery** to review suggestions. Show that an action requires an explicit approval.
4. Visit **AI Cost** to compare spend, token usage, cache performance, and models.
5. Ask a question in **AI Workspace** and point out the model and cost details on the response.
6. Finish in **Settings** with language, theme, budget, and PayPal connection options.

## Run the presentation build

The frontend can be presented with its bundled local dataset; a PayPal account, API key, or backend is not
required for this mode. The interface intentionally stays focused on the product rather than displaying a
data-source badge. The README documents the demo mode so that its figures are not mistaken for verified
business results.

Requirements: a Node.js version supported by Vite 8 and npm.

```bash
cd frontend
npm install
npm run dev
```

Open `http://localhost:5173`. The default data source is the local demo adapter. To set it explicitly, create
`frontend/.env` with:

```env
VITE_DATA_SOURCE=mock
```

Restart the development server after changing environment variables. Demo interactions are local to the
frontend; they do not charge a customer or send a real PayPal reminder.

## Connect a backend or PayPal Sandbox

The backend is an ASP.NET Core 8 API backed by PostgreSQL with pgvector. A local Docker Compose setup is
provided in [`docker-compose.yml`](./docker-compose.yml); configure its required database and administrator
secrets in a private root `.env` file before starting it. Never commit secrets or paste them into screenshots,
recordings, or source control.

To point the frontend at the API, set the following in `frontend/.env` and restart Vite:

```env
VITE_DATA_SOURCE=real
VITE_API_BASE_URL=http://localhost:5000/api/v1
```

Sign in with an administrator account to manage PayPal Sandbox credentials in **Settings**. Credentials are
stored encrypted by the backend. A successful OAuth test confirms API authentication; transaction access still
depends on the Sandbox app's permissions. Webhook setup is optional for manual synchronization. For deployment
and endpoint details, see the [API contract](./docs/API_CONTRACT.md).

## Architecture

```text
frontend/   React, TypeScript, Vite, Tailwind CSS
backend/    ASP.NET Core 8 API, application/domain/infrastructure layers
docs/       API contract and implementation notes
```

The frontend selects a data adapter from environment configuration and uses shared query hooks to load and
update data. The backend owns financial calculations and persists revenue, recovery, AI gateway, and sync
records. The frontend formats reported values; it does not invent savings or independently calculate business
metrics.

## Intended outcome

Recoup aims to give revenue and product teams one understandable operating picture: identify payment issues
sooner, evaluate recovery actions before sending them, and make AI spending easier to inspect and optimize.
The current demo illustrates that workflow; any measured business impact must be validated against real
PayPal and AI-provider data.
