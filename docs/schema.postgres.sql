-- RevenuePilot AI: initial PostgreSQL schema (first release only; no unused tables).
-- Money is numeric(18,2). Timestamps are UTC (timestamptz).
-- Tenant isolation: every organisation-owned row carries organisation_id, and child tables use
-- composite foreign keys so a row can never point at another organisation's parent.
-- Still enforce the organisation filter in the API; the database is a second safety net.

create table organisations (
  id uuid primary key default gen_random_uuid(),
  name text not null,
  business_type text,
  currency char(3) not null default 'ZAR',
  timezone text not null default 'Africa/Johannesburg',
  created_at timestamptz not null default now()
);

create table app_users (
  id uuid primary key default gen_random_uuid(),
  email text not null unique,
  display_name text,
  created_at timestamptz not null default now()
  -- password hashing handled by ASP.NET Core Identity (its own tables)
);

create table organisation_memberships (
  organisation_id uuid not null references organisations(id) on delete cascade,
  user_id uuid not null references app_users(id) on delete cascade,
  role text not null check (role in ('owner','member')),
  primary key (organisation_id, user_id)
);

create table customers (
  id uuid primary key default gen_random_uuid(),
  organisation_id uuid not null references organisations(id) on delete cascade,
  external_id text,
  name text not null,
  email text,
  segment text,
  status text not null default 'active' check (status in ('active','archived')),
  is_demo boolean not null default false,
  created_at timestamptz not null default now(),
  unique (organisation_id, id),
  unique (organisation_id, external_id)
);

create table transactions (
  id uuid primary key default gen_random_uuid(),
  organisation_id uuid not null,
  customer_id uuid not null,
  external_id text not null,
  occurred_at timestamptz not null,
  amount numeric(18,2) not null,
  currency char(3) not null default 'ZAR',
  status text not null check (status in ('paid','refunded','cancelled')),
  description text,
  import_id uuid,
  created_at timestamptz not null default now(),
  foreign key (organisation_id, customer_id) references customers (organisation_id, id) on delete cascade,
  unique (organisation_id, external_id)  -- duplicate transaction IDs are rejected per organisation
);
create index ix_tx_org_date on transactions (organisation_id, occurred_at);
create index ix_tx_customer_date on transactions (organisation_id, customer_id, occurred_at);

create table data_imports (
  id uuid primary key default gen_random_uuid(),
  organisation_id uuid not null references organisations(id) on delete cascade,
  kind text not null check (kind in ('customers','transactions')),
  file_name text not null,
  rows_total int not null default 0,
  rows_imported int not null default 0,
  rows_rejected int not null default 0,
  error_report jsonb,
  created_by uuid references app_users(id),
  created_at timestamptz not null default now(),
  unique (organisation_id, id)
);

create table revenue_opportunities (
  id uuid primary key default gen_random_uuid(),
  organisation_id uuid not null,
  customer_id uuid,
  type text not null,
  reason text not null,
  estimated_value numeric(18,2) not null default 0,        -- an estimate
  recovered_value numeric(18,2) not null default 0,        -- only set from a user-recorded outcome
  priority text not null check (priority in ('low','medium','high')),
  status text not null default 'new' check (status in ('new','in_progress','won','lost','dismissed')),
  owner_user_id uuid references app_users(id),
  follow_up_on date,
  outcome_notes text,
  created_at timestamptz not null default now(),
  foreign key (organisation_id, customer_id) references customers (organisation_id, id),
  unique (organisation_id, id)
);

create table opportunity_activities (  -- action history: what was done and what resulted
  id uuid primary key default gen_random_uuid(),
  organisation_id uuid not null,
  opportunity_id uuid not null,
  action text not null,
  note text,
  created_by uuid references app_users(id),
  created_at timestamptz not null default now(),
  foreign key (organisation_id, opportunity_id) references revenue_opportunities (organisation_id, id) on delete cascade
);

create table subscriptions (
  organisation_id uuid primary key references organisations(id) on delete cascade,
  plan text not null check (plan in ('starter','growth','professional')),
  status text not null default 'development' check (status in ('development','active','past_due','cancelled')),
  provider_reference text,  -- set only after verified provider confirmation
  renews_on date
);

create table audit_logs (
  id bigint generated always as identity primary key,
  organisation_id uuid references organisations(id) on delete set null,
  user_id uuid references app_users(id) on delete set null,
  action text not null,
  detail jsonb,             -- never store secrets or unnecessary personal data
  created_at timestamptz not null default now()
);
create index ix_audit_org_date on audit_logs (organisation_id, created_at);

-- Added in later stages: invoices, customer_activities, conversations, conversation_messages, revenue_insights.
