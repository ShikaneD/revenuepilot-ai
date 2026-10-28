# RevenuePilot AI: Build Specification (reconciled)

**Tagline:** Turn lost revenue into your next growth opportunity.
**Product:** AI-powered revenue intelligence and customer retention SaaS for small and medium-sized businesses.
**Status of this document:** This is the only spec. Ignore any earlier version or pasted prompt.

---

## 0. How Claude Code must work

1. Read this file fully, then inspect the repository before changing anything.
2. Show a short plan before writing code for each phase.
3. Work one phase at a time (section 17). Stop at the end of each phase and give the report in section 18. Do not start the next phase until the user says so.
4. Run builds and tests after each phase and show the real output. Never claim a feature works unless it was built and verified.
5. Make sensible technical decisions without asking about every detail. Ask only when something blocks progress.
6. Never ask the user to paste a password, API key or token into the chat. Give them the command to run themselves.
7. Fix errors properly. Do not hide them with broad exception handling.
8. Keep a running `docs/PROGRESS.md` with completed work, remaining work and known limitations.

---

## 1. Current state (already done, do not redo)

**Machine:** Windows 10/11, PowerShell, VS Code. **Docker cannot run on this machine.**

**Installed:** Git, Node.js 24 LTS, .NET SDK 10.0 (10.0.403), PostgreSQL 16 (Windows service `postgresql-x64-16`), DBeaver, VS Code with C# Dev Kit, ESLint, Tailwind and Prettier extensions, Claude Code.

**Database:** local PostgreSQL 16 on `localhost:5432`, database `revenuepilot`, user `postgres`. The database exists and should be treated as empty. The password is held by the user. Store the connection string with .NET user secrets. Never write it into a committed file.

**Repository:** `C:\dev\revenuepilot`, Git initialised, one commit made.
- `backend/src/RevenuePilot.Domain/Churn/ChurnScorer.cs`: working rule-based scorer (section 7).
- `backend/tests/RevenuePilot.Tests/ChurnScorerTests.cs`: 12 xUnit tests, all passing on .NET 10.
- `docs/schema.postgres.sql`: an early hand-written schema. **It is superseded.** EF Core migrations become the schema source of truth. Delete this file once the first migration exists.
- `.gitignore` (dotnet template).

**Existing design reference (optional):** a single-file HTML prototype of the landing page and app shell exists outside the repo. It showed these views: Overview, Customers, Churn risk, Revenue opportunities and AI analyst, on synthetic data. Use it only as visual inspiration. Do not copy its in-browser calculations.

---

## 2. Environment rules

- Everything must run locally with `dotnet run` and `npm run dev` against local PostgreSQL. No step may require Docker.
- Still write the Dockerfiles and `docker-compose.yml` as deliverables, but mark them **untested on this machine** in the README.
- Target the current .NET LTS (net10.0). Pin package versions. Do not use packages published in the last 14 days.
- Every command in the README must work in Windows PowerShell. Give PowerShell syntax, not bash.
- Do not add microservices, Kubernetes, RabbitMQ or a Python service. Build a modular monolith. Defer Hangfire until scheduled work actually needs it.

---

## 3. Product vision

RevenuePilot AI helps a business owner see what is happening to revenue, why, which customers or products need attention, how much could be recovered, and what to do next.

Core principle: **Detect a problem, explain the evidence, estimate the financial impact, recommend an action, track the outcome.**

Differentiator: it connects analysis, explainable insights, prioritised opportunities and actions in one place. Do not claim the concept is unique in the market.

Initial users: service businesses, subscription businesses, e-commerce and companies with repeat customers. Default currency is South African rand (ZAR); the data model must support other currencies later.

---

## 4. Technology stack

**Frontend:** React, TypeScript (strict), Vite, Tailwind CSS, shadcn/ui, React Router, TanStack Query, React Hook Form with Zod, Recharts, Lucide icons, Vitest with React Testing Library.

**Backend:** C#, ASP.NET Core Web API, Entity Framework Core (Npgsql), FluentValidation, Swagger/OpenAPI, structured logging, dependency injection, xUnit.

**Database:** PostgreSQL 16. This decision is final; document it in the README.

**AI:** a provider abstraction in the backend. Keys come only from backend environment variables or user secrets, never the browser. The app must work fully with no AI key.

**Authentication:** ASP.NET Core Identity with secure cookie or token handling. Never store tokens in localStorage. Choose one design and document it.

**CI:** GitHub Actions (restore, build, test, lint, frontend build).

---

## 5. Architecture and repository layout

```text
revenuepilot/
├── frontend/              React app (features/ folders: dashboard, customers, opportunities, ai-analyst, imports, reports)
├── backend/
│   ├── src/
│   │   ├── RevenuePilot.Api/             endpoints, middleware, auth config, DTOs
│   │   ├── RevenuePilot.Application/     use cases, validation, orchestration
│   │   ├── RevenuePilot.Domain/          entities and pure business rules (EXISTS: Churn/)
│   │   └── RevenuePilot.Infrastructure/  EF Core, AI providers, external services
│   └── tests/RevenuePilot.Tests/         xUnit (EXISTS)
├── docs/                  SPEC.md, PROGRESS.md, architecture and deployment notes
├── .github/workflows/
├── docker-compose.yml     (untested here)
├── .env.example           placeholders only
└── README.md
```

Rules: business logic stays out of controllers; use DTOs, not entities, in API responses; async database and network calls; no unnecessary abstraction.

---

## 6. Product features

### 6.1 Revenue dashboard
KPIs: total revenue, growth versus the previous equivalent period (show the comparison dates), revenue at risk, estimated recoverable revenue, customers at risk, repeat purchase rate, average order value, returning-customer revenue, refunds and failed payments, opportunities needing attention.
Charts: revenue over time, current versus previous period, revenue by product, customers by risk level, revenue at risk by segment, opportunities by value, recent activity.
Period selector: last 7, 30 and 90 days, and a custom range. All metrics and charts update together. Every metric has a label, formatting and a tooltip explaining its calculation.

### 6.2 Customers and churn risk
Searchable, filterable (risk level, segment), sortable (revenue at risk), paginated table. Customer detail page with purchase history, revenue trend, risk explanation, related opportunities, follow-up history. Create, edit, view and archive customers with server-side validation.
For each customer show: segment, last purchase date, historical revenue, order count, average order value, spending trend, expected next purchase date (only if reliably estimable), risk level and score, estimated revenue at risk (labelled an estimate), reasons, recommended action.

### 6.3 Revenue leakage
Flag only what the data supports: failed payments, refunds and cancellations, unpaid invoices (only if invoice data exists), spending declines, missed repeat purchases, products with falling revenue, unusual changes in sales. Each finding includes category, description, evidence, period, affected customer or product, financial value, confidence, suggested action and status. Do not infer abandoned orders without checkout events. Do not count one loss in several findings. Separate **confirmed losses**, **estimated revenue at risk** and **possible future opportunity**. Distinguish observed facts from inferred causes.

### 6.4 Opportunities
Prioritised action list with title, type, customer or product, reason, estimated impact, priority, supporting data, next action, status, created and completed dates. Priority combines financial impact, urgency, evidence quality and actionability, not value alone.
Statuses: New, In progress, Action required, Recovered, Dismissed. Log every status change. **Estimated value and recovered value are separate fields.** Revenue counts as recovered only when the user records a verified outcome or transaction data supports it. Keep an action history so results can be measured.

### 6.5 AI analyst
Conversation with history, suggested questions, loading state, empty-prompt and error handling, new-conversation action. Answers show the figures used and the date range, cite internal metric names or records, and label facts, estimates, assumptions and recommendations. If data is insufficient, say so. Without a configured provider, show a clearly labelled **Demo analysis** mode built from deterministic calculations. Never present canned text as model output.

### 6.6 Follow-up message generator
Types: retention email, follow-up, payment reminder, re-engagement, repeat-purchase reminder, general check-in. Tones: professional, friendly, concise. Generate, edit, copy, save, regenerate. Never send automatically. Do not invent personal details or promises.

### 6.7 CSV import
Upload, validate, preview, map columns, show errors, confirm, import, summary of imported, skipped and rejected rows. Customers and orders first. Validate type and size, dates, amounts and identifiers. Reject invalid money values. Detect duplicates, and make re-imports idempotent (unique per business on external ID). Define handling of refunds, cancellations, missing customers and currencies. Track import history, offer an error report and a sample template. Never execute or trust uploaded content.

### 6.8 Reports
Revenue performance, retention and inactivity, revenue at risk, opportunities and outcomes, product performance, refunds and payment issues. Date range filter, report generated date, stated limitations, CSV export with tenant and role checks. Structure so PDF export can be added later.

### 6.9 Public website and app pages
Landing page: nav (Product, Features, How it works, Pricing, Sign in, Get started); hero; problem; solution; how it works; feature showcase; pricing; trust and privacy; final call to action; footer with privacy, terms, contact placeholders and copyright.
**Hero headline:** "Turn lost revenue into your next growth opportunity."
**Supporting text:** "RevenuePilot AI helps you understand declining sales, identify customers at risk, uncover revenue opportunities, and take action using AI-powered insights."
**CTAs:** "Get Started" and "Explore Demo".
Auth pages: sign up, sign in, sign out, forgot and reset password, access denied, session expiry.
Onboarding: business name, industry, business model, currency, size (optional), reporting period, then import data or load sample data. Resumable.
App shell: collapsible sidebar (Overview, AI Analyst, Customers, Revenue Leakage, Opportunities, Reports, Data Imports, Settings); header with business, reporting period, notifications, user menu. Settings: business profile, user profile, reporting preferences, currency, notifications, data management, security, billing, privacy and account deletion.
Every visible control must work or be clearly marked unavailable.

### 6.10 Truthfulness rule
No fabricated testimonials, customer logos, usage numbers, certifications or revenue-improvement claims. Illustrative content must be labelled.

---

## 7. Churn risk scoring (extend, do not rewrite)

Reuse `ChurnScorer` and keep its 12 tests passing. Document it in `docs/`.

- Score 0 to 100 = recency (max 60) + spend decline (max 30) + frequency decline (max 10), rounded.
- **Recency:** clamp((days since last purchase / average gap − 1) × 30, 0, 60).
- **Spend decline:** recent average of the last min(3, n/2) orders versus the earlier average; points = min(30, decline × 60).
- **Frequency decline:** purchases in the last 90 days versus the 90 days before; points = min(10, decline × 20).
- Bands (configurable): low below 35, medium 35 to 59, high 60 and above.
- No score when a customer has fewer than 3 paid transactions.
- Exposure estimate = average order value × (30 / average gap) × 3 months. Always labelled an estimate and assumes the customer continues at their historical pace.
- Only paid transactions are passed in. Refunds and cancellations are excluded before scoring.
- The score is a **risk indicator**, not a validated churn probability. Never claim certainty a customer will leave.
- Extend by adding: configurable options through the application configuration, a stored scoring version, and an expected-next-purchase estimate. Add tests for any new rule.

---

## 8. Financial definitions (document each in the README)

- **Net revenue** = sum of totals of paid orders minus refund amounts, in the business currency. Cancelled and unpaid orders are excluded. Use the term consistently everywhere.
- **Growth %** = (current − previous) / previous × 100. If previous is zero, return "not applicable", never a divide-by-zero.
- **Average order value** = net revenue of qualifying orders / number of qualifying orders.
- **Repeat purchase rate** and **returning-customer revenue:** a returning customer has a qualifying order before the period start. Document the exact rule.
- **Revenue at risk:** the sum of per-customer exposure estimates for customers in medium or high risk, counted once per customer. Never sum lifetime value.
- **Estimated recoverable revenue** = revenue at risk × a configurable recovery assumption (the default is a placeholder and must be labelled as an assumption).
- **Confirmed leakage:** only losses the records prove (failed payments, refunds, cancellations). Never added to revenue at risk.
- Money is `decimal` in C# and `numeric(18,2)` in PostgreSQL. Never floating point.

---

## 9. Data model (EF Core migrations are the source of truth)

Use these entities, adding each only when its phase needs it: Business, User (via Identity), BusinessMembership (roles: owner, member), Customer, Product, Order, OrderItem, CustomerRiskAssessment, RevenueIssue, Opportunity, FollowUpActivity, ImportJob, AIConversation, AIMessage, Subscription, AuditEvent. Add Invoice only when invoice import is built.

Rules:
- Every business-owned row carries `BusinessId`. Use composite relationships where practical so a child row cannot reference another business's parent.
- Unique constraints per business on external customer, order and product IDs (this is what makes imports idempotent).
- All timestamps `timestamptz` in UTC.
- Sensible indexes on (BusinessId, date) and (BusinessId, CustomerId, date).
- Archive customers instead of hard deleting where history matters.

---

## 10. Dates and time (required)

- Never call `DateTime.UtcNow` directly in business logic. Inject `TimeProvider`. Tests use a fixed time.
- Period boundaries ("last 30 days", "this month") are calculated in the **business's timezone** (default `Africa/Johannesburg`), then converted to UTC for queries.
- Demo data is generated **relative to the current date** (for example "30 days ago") so the demo never goes stale. It must be deterministic in shape (same customers, same pattern) using a fixed random seed.
- `created_at` columns default to the database's UTC `now()`. `occurred_at` on orders comes from the data, never a default.

---

## 11. Security and privacy

Server-side validation; authentication and role-based authorisation; **tenant isolation enforced on the server**. Never trust a business ID from the browser as proof of access; always verify membership. Rate limiting on sensitive and AI endpoints; safe error messages; CORS configured; security headers; secure file upload handling; structured logs without secrets or unnecessary personal data; audit events for sensitive actions; data retention, export and deletion workflows.

**AI security:** treat imported content and prompts as untrusted data. Send the minimum data needed. Calculate all figures in application code and pass them to the model as evidence. The model explains and recommends but never invents or redefines figures. Timeouts and cost controls on AI calls.

**POPIA:** design with the Protection of Personal Information Act in mind and list what needs professional legal review before real customer data is accepted. Never claim the product is legally compliant or security-certified. Privacy policy and terms are placeholders requiring review.

**Secrets:** user secrets locally; environment variables or a managed secret store in Azure. `.env.example` has placeholders only. No secret is ever committed.

---

## 12. AI integration

- Interface (for example `IAiProvider`) with a real provider implementation, a deterministic demo provider and a mock provider for tests.
- Context builder gathers authorised, organisation-scoped, date-filtered metrics before any model call.
- Validate structured model responses; handle failures with a useful message. Core analytics must keep working if the AI provider is down.
- Demo mode is labelled everywhere it appears.

---

## 13. Subscriptions and billing readiness

Plans (proposed starting prices to test with customers, not validated, configurable): **Starter R299/month**, **Growth R699/month**, **Professional R1,499/month**. Make limits configurable and enforce them server-side. Billing page shows plan, usage, renewal status and upgrade options. Payment provider sits behind an interface. Without live credentials, use a clearly labelled **development billing mode**. Never simulate a successful real payment or activate a paid plan without verified provider confirmation. Build no fake checkout.

---

## 14. Design system

Premium, restrained B2B analytics look. Deep navy or charcoal sidebar, light content area, one blue or indigo accent, clear success, warning and error colours. Light and dark themes. Reusable components; clear loading, empty, error and success states; confirmation for destructive actions; responsive on desktop, tablet and mobile; semantic HTML, keyboard navigation, visible focus, WCAG-minded contrast. **Never rely on colour alone to show risk** (always include a text label).

---

## 15. Demo data

A fictional South African business, ZAR, synthetic people only. At least 30 customers; several months of orders; multiple products or services; returning customers; declining-frequency customers; inactive customers; a few failed payments and refunds; a range of risk scores; opportunities in several statuses; historical follow-ups. Coherent relationships throughout. Clearly labelled **Demo data** in the UI. Replaceable by imported data. Dashboard figures are calculated from the stored rows.

---

## 16. Testing

Backend (xUnit): revenue calculations, period growth including the zero-previous case, refund handling, churn scoring (keep the existing 12), opportunity prioritisation, validation rules, tenant isolation and authorisation, CSV validation and duplicate handling, API error responses, AI response validation and fallback (mock provider, no paid calls). Add integration tests for key API flows. Test real business rules and failure cases, not just non-null results.
Frontend (Vitest and React Testing Library): critical forms, dashboard loading and error states, customer filters, opportunity status changes, AI analyst interface, API error handling.
Provide a working health-check endpoint including a database check.

---

## 17. Phases (stop after each)

1. **Foundation:** add the Api, Application and Infrastructure projects around the existing Domain and Tests projects; frontend scaffold; design system and routing; EF Core and the first migration; user-secrets connection string; health check; README with PowerShell run commands; Dockerfiles written but untested. The existing 12 tests still pass.
2. **Landing page, app shell and demo data:** public site, authenticated shell, seed script (dates relative to now), dashboard reading from the API.
3. **Customer intelligence:** customers, details, risk scoring wired to the database, filters and search, tests.
4. **Opportunities and leakage:** detection, prioritisation, lifecycle, follow-ups, recovered-versus-estimated tracking.
5. **AI analyst and message generator:** provider abstraction, demo fallback, validation.
6. **Import and reports:** CSV workflow, duplicate handling, exports.
7. **Authentication and hardening:** Identity, tenant isolation proof, rate limiting, audit events, privacy workflows.
8. **Testing and deployment:** CI, Azure guidance, final checks.

**Phase 1 verification step:** after seeding in Phase 2, pick one revenue total shown by the API and show how to confirm it by summing the matching rows in DBeaver or `psql`. A phase is not done until the number matches.

---

## 18. Report required at the end of every phase

What was implemented; files and modules created or changed; exact PowerShell commands to run it; tests executed with real results; what remains incomplete; any credentials or configuration the user still needs to supply. Then commit: `git add .` and `git commit -m "Phase N: <summary>"`.

---

## 19. Acceptance criteria (full product)

Polished landing page and working navigation; coherent demo dashboard with working date filters; searchable customers with explainable risk; evidence-linked leakage findings; opportunities created, updated and resolved with recovered value kept separate from estimates; editable and copyable follow-up drafts; validated CSV import with tested duplicate handling; consistent report calculations; server-side AI credentials with demo mode clearly distinguished; data persisted in PostgreSQL; server-enforced tenant isolation; loading, empty, validation and error states; automated tests covering the key business rules; documented local run (without Docker), Docker files, CI and Azure deployment; README covering setup, architecture, environment variables, testing and deployment; no secrets, fabricated testimonials or unsupported claims in the repository; a list of known limitations and remaining production-readiness tasks.

---

## 20. First task

Start Phase 1. Inspect the repository, then show your plan. Confirm the existing tests still pass before changing anything. Do not begin Phase 2 until told.
