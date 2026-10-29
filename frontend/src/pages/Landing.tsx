import { Link } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { fetchSummary } from '../lib/api'
import { formatMoney } from '../lib/format'

const FEATURES = [
  { name: 'Revenue intelligence', text: 'Net revenue, growth against the previous period, average order value, refunds and failed payments, calculated from your orders.', status: 'In the demo' },
  { name: 'Customer churn risk', text: 'A 0–100 risk indicator for each customer, with the evidence behind it. A flag to investigate, not a prediction.', status: 'In the demo' },
  { name: 'Revenue leak detection', text: 'Spot falling products, missed repeat purchases and unusual changes in sales.', status: 'Planned' },
  { name: 'AI business analyst', text: 'Ask questions in plain English. Figures come from calculated metrics, never from the language model.', status: 'Planned' },
  { name: 'Recovery opportunities', text: 'A prioritised action list with an estimated value, and a record of what was actually recovered.', status: 'Planned' },
  { name: 'AI follow-up assistant', text: 'Draft customer messages that you review and edit. Nothing is sent automatically.', status: 'Planned' },
]

const PLANS = [
  { name: 'Starter', price: 299, text: 'For small businesses starting to track revenue performance.' },
  { name: 'Growth', price: 699, text: 'For growing businesses that need more detailed analysis.' },
  { name: 'Professional', price: 1499, text: 'For businesses with more advanced analytical needs.' },
]

function HeroPreview() {
  const { data } = useQuery({ queryKey: ['summary', 30], queryFn: () => fetchSummary(30), retry: false })
  if (!data) return null
  return (
    <div className="rounded-lg border border-slate-200 bg-white p-4 shadow-sm">
      <div className="text-xs text-slate-500">
        <span className="rounded-full bg-amber-100 px-2 py-0.5 font-medium text-amber-900">Demo data</span> Live from the demo database, last 30 days
      </div>
      <div className="mt-3 grid grid-cols-3 gap-3 text-sm">
        <div><div className="text-slate-600">Net revenue</div><div className="font-semibold">{formatMoney(data.netRevenue, data.business.currency)}</div></div>
        <div><div className="text-slate-600">Growth</div><div className="font-semibold">{data.growthPercent === null ? 'n/a' : `${data.growthPercent}%`}</div></div>
        <div><div className="text-slate-600">Paid orders</div><div className="font-semibold">{data.orderCount}</div></div>
      </div>
    </div>
  )
}

export default function Landing() {
  const section = 'scroll-mt-16 border-t border-slate-200 py-12'
  return (
    <div className="min-h-screen bg-white text-slate-900">
      <header className="sticky top-0 z-10 border-b border-slate-200 bg-white/95">
        <div className="mx-auto flex h-14 max-w-5xl items-center gap-5 px-6">
          <span className="text-lg font-semibold">RevenuePilot AI</span>
          <nav aria-label="Main" className="ml-auto hidden gap-5 text-sm text-slate-700 sm:flex">
            <a href="#features">Features</a>
            <a href="#how">How it works</a>
            <a href="#pricing">Pricing</a>
            <a href="#trust">Trust</a>
          </nav>
          <Link to="/app" className="rounded-md bg-indigo-700 px-3 py-1.5 text-sm font-medium text-white sm:ml-0">Explore Demo</Link>
        </div>
      </header>

      <main className="mx-auto max-w-5xl px-6">
        <section className="grid items-center gap-8 py-14 md:grid-cols-2">
          <div>
            <h1 className="text-4xl font-bold leading-tight">Turn lost revenue into your next growth opportunity.</h1>
            <p className="mt-4 text-lg text-slate-700">
              RevenuePilot AI helps you understand declining sales, identify customers at risk, uncover revenue opportunities, and take action using AI-powered insights.
            </p>
            <div className="mt-6 flex flex-wrap gap-3">
              <Link to="/app" className="rounded-md bg-indigo-700 px-5 py-2.5 font-medium text-white">Explore Demo</Link>
              <a href="#how" className="rounded-md border border-slate-300 px-5 py-2.5 font-medium text-slate-800">How it works</a>
            </div>
            <p className="mt-3 text-xs text-slate-500">This is an early prototype. Accounts and data import are not available yet.</p>
          </div>
          <HeroPreview />
        </section>

        <section className={section}>
          <h2 className="text-2xl font-semibold">Revenue rarely drops all at once</h2>
          <p className="mt-3 max-w-3xl text-slate-700">
            Customers buy a little less often. Orders get smaller. Payments fail and nobody follows up. Each change is small, spread across spreadsheets and reports, and easy to miss. Together they add up to revenue you did not know you were losing.
          </p>
        </section>

        <section id="features" className={section}>
          <h2 className="text-2xl font-semibold">What it does</h2>
          <div className="mt-5 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
            {FEATURES.map((f) => (
              <div key={f.name} className="rounded-lg border border-slate-200 p-4">
                <div className="flex items-center justify-between gap-2">
                  <h3 className="font-semibold">{f.name}</h3>
                  <span className={`rounded-full px-2 py-0.5 text-xs font-medium ${f.status === 'Planned' ? 'bg-slate-100 text-slate-700' : 'bg-emerald-100 text-emerald-900'}`}>{f.status}</span>
                </div>
                <p className="mt-2 text-sm text-slate-700">{f.text}</p>
              </div>
            ))}
          </div>
        </section>

        <section id="how" className={section}>
          <h2 className="text-2xl font-semibold">How it works</h2>
          <ol className="mt-5 grid gap-4 sm:grid-cols-3">
            <li className="rounded-lg border border-slate-200 p-4"><strong>1. Connect or import your data.</strong><p className="mt-1 text-sm text-slate-700">CSV import is planned. The demo uses a fictional business.</p></li>
            <li className="rounded-lg border border-slate-200 p-4"><strong>2. Analyse revenue and behaviour.</strong><p className="mt-1 text-sm text-slate-700">Revenue and customer risk are calculated from the order history.</p></li>
            <li className="rounded-lg border border-slate-200 p-4"><strong>3. Prioritise and act.</strong><p className="mt-1 text-sm text-slate-700">Action tracking is planned for a later phase.</p></li>
          </ol>
        </section>

        <section id="pricing" className={section}>
          <h2 className="text-2xl font-semibold">Pricing</h2>
          <p className="mt-2 text-sm text-slate-600">Proposed starting prices, not yet validated with customers. Billing is not available in this prototype.</p>
          <div className="mt-5 grid gap-4 sm:grid-cols-3">
            {PLANS.map((p) => (
              <div key={p.name} className="rounded-lg border border-slate-200 p-4">
                <h3 className="font-semibold">{p.name}</h3>
                <div className="mt-1 text-2xl font-bold">R{p.price.toLocaleString('en-ZA')}<span className="text-sm font-normal text-slate-600">/month</span></div>
                <p className="mt-2 text-sm text-slate-700">{p.text}</p>
              </div>
            ))}
          </div>
        </section>

        <section id="trust" className={section}>
          <h2 className="text-2xl font-semibold">Trust and transparency</h2>
          <div className="mt-5 grid gap-4 sm:grid-cols-3">
            <div className="rounded-lg border border-slate-200 p-4"><h3 className="font-semibold">Your data</h3><p className="mt-1 text-sm text-slate-700">This prototype does not accept real customer data. A privacy policy and a POPIA review are required before it does.</p></div>
            <div className="rounded-lg border border-slate-200 p-4"><h3 className="font-semibold">Estimates are labelled</h3><p className="mt-1 text-sm text-slate-700">Revenue at risk is an estimate. Recovered revenue will only count outcomes you record.</p></div>
            <div className="rounded-lg border border-slate-200 p-4"><h3 className="font-semibold">You stay in control</h3><p className="mt-1 text-sm text-slate-700">The product recommends. It never contacts a customer on its own.</p></div>
          </div>
        </section>
      </main>

      <footer className="border-t border-slate-200 py-8 text-sm text-slate-600">
        <div className="mx-auto max-w-5xl px-6">
          © 2026 RevenuePilot AI. Prototype with synthetic data. Contact: [contact email placeholder] · Privacy policy [placeholder] · Terms of service [placeholder]
        </div>
      </footer>
    </div>
  )
}