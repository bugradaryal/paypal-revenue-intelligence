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
- **AI Revenue Brief:** server-calculated payment metrics, failure patterns, previous-period comparisons, and validated AI commentary with a rule-based fallback.
- **Payment investigation:** searchable and filterable payment records with status history and failure details.
- **Recovery Command Center:** the single recovery workspace for open issues, AI-assisted proposals, review and execution states, errors, and recovery evidence from refreshed source records.
  Advice-only recommendations can be marked reviewed; only eligible PayPal invoice reminders can be explicitly confirmed and sent.
- **Cash Flow Outlook:** invoice-backed open balances, partial payments, overdue aging, a 13-week USD due-date schedule, and separate original-currency exposure.
- **Payment outcome breakdown:** an Overview widget separates completed, failed, pending, and refunded USD activity without presenting failed attempts as proven lost revenue.
- **Payment failure intelligence:** Payments includes selected-period failure trends and reason totals; counts cover all currencies while amounts are USD-only.
- **Customer Cohorts:** a distinct view groups customers by first successful USD payment month and shows subsequent payment activity and revenue per cohort customer.
- **Revenue Signals:** a live attention queue for overdue and soon-due invoices, unresolved recovery issues, pending reviews, failed actions, foreign-currency exposure, and rule-based anomalies. It refreshes while open; it is not a persisted notification system.
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

1. Open **Overview**, select a time range, and generate the AI Revenue Brief to show grounded metrics, failure patterns, and the previous-period comparison.
2. Go to **Payments** and inspect a failed payment, its reason, and status history.
3. Open **Recovery Command Center** to separate open source-record issues from proposed actions, review a suggestion, and see whether later PayPal sync data confirms recovery.
4. Visit **Cash Flow Outlook** to show partial-payment balances, overdue aging, upcoming invoice due dates, and non-USD balances kept in their original currencies.
5. On **Overview**, inspect the payment outcome breakdown; in **Payments**, compare failure reasons over the selected period.
6. Open **Customer Cohorts** for first-payment retention patterns, then **Revenue Signals** for anomalies and an attention queue linking urgent items to their operational pages.
7. Visit **AI Cost** to compare spend, token usage, cache performance, and models.
8. Ask a question in **AI Workspace** and point out the model and cost details on the response.
9. Finish in **Settings** with language, theme, budget, and PayPal connection options.

## Run the presentation build

The frontend can be presented with its bundled fictional dataset; a PayPal account, API key, or backend is not
required for this mode. The Overview, Recovery Command Center, Cash Flow Outlook, Customer Cohorts, and Revenue Signals pages label
demo analysis and records. Payment outcome analysis surfaces exposure rather than claiming failed attempts are
lost revenue; cohort activity is not subscription churn, and cancellations are not inferred from transaction data. Cash-flow examples are
fictional; live USD balances are calculated by the backend from
open invoices less linked completed payments. Other currencies are displayed separately without an assumed
exchange rate. Demo recommendations can be reviewed locally but never call PayPal or simulate a successful
external action. These figures are not verified business results.

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
VITE_AI_SOURCE=real
```

The default keeps business data fictional while using the authenticated backend for live AI responses.
Start the backend and sign in to use AI; each answer can incur provider costs. To run a fully offline demo
with predefined assistant responses, set `VITE_AI_SOURCE=mock` instead. Restart the development server
after changing environment variables. Demo interactions never charge a customer or send a real PayPal reminder.

## Connect a backend or PayPal Sandbox

The backend is an ASP.NET Core 8 API backed by PostgreSQL with pgvector. A local Docker Compose setup is
provided in [`docker-compose.yml`](./docker-compose.yml); configure its required database and administrator
secrets in a private root `.env` file before starting it. Never commit secrets or paste them into screenshots,
recordings, or source control.

To point the frontend at the API, set the following in `frontend/.env` and restart Vite:

```env
VITE_DATA_SOURCE=real
VITE_AI_SOURCE=real
VITE_API_BASE_URL=http://localhost:5000/api/v1
```

Sign in with an administrator account to manage PayPal Sandbox credentials in **Settings**. Credentials are
stored encrypted by the backend. A successful OAuth test confirms API authentication; transaction access still
depends on the Sandbox app's permissions. Webhook setup is optional for manual synchronization. For deployment
and endpoint details, see the [API contract](./docs/API_CONTRACT.md).

## AI providers and model selection

The gateway currently supports OpenAI and Google Gemini. For local Gemini access, add the key to
`backend/src/PRI.Api/appsettings.Development.json` (this file is local-only and must not be committed):

```json
{
  "Gemini": {
    "GeminiKey": "YOUR_GEMINI_API_KEY"
  }
}
```

Alternatively, store it with .NET User Secrets:

```powershell
dotnet user-secrets set "Gemini:GeminiKey" "<YOUR_GEMINI_API_KEY>" --project backend/src/PRI.Api
```

The model catalog seeds `gemini-3.5-flash-lite`. In **AI Workspace**, choose **Auto**, **OpenAI**, or **Gemini**.
Auto infers common summary, classification, and analysis tasks from the latest prompt, then applies the
matching routing rule (or the default rule). It ranks the rule's models and the best eligible model from each
enabled provider by task fit, then catalog token prices; the configured primary is not forced to run first.
Choosing OpenAI or Gemini restricts automatic model selection to that provider. The budget is a spending cap,
not a model-selection signal; requests stop when the cap is reached. The current rules contribute OpenAI GPT-6
Luna for default, classification, and summarization, and GPT-6.1 Terra for analysis. If a selected provider is
not configured or is temporarily unavailable, Auto can try the next ranked candidate. The response reports
which provider/model actually answered.

The AI Cost page calculates Gemini estimates from Google's published paid-tier token prices stored in the
catalog; actual billing can differ by account tier and usage. Review current [Gemini API pricing](https://ai.google.dev/gemini-api/docs/pricing)
before relying on cost estimates.

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
