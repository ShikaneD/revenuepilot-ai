import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { fetchRisk } from '../lib/api'
import { formatDate, formatMoney } from '../lib/format'
import RiskBadge from '../components/RiskBadge'

export default function RiskPage() {
  const { data, isLoading, error } = useQuery({ queryKey: ['risk'], queryFn: fetchRisk })
  const money = (n: number) => formatMoney(n, data?.business.currency)

  return (
    <div className="mx-auto max-w-5xl">
      <div className="flex flex-wrap items-center gap-3">
        <h1 className="text-xl font-semibold">Churn risk</h1>
        {data?.business.isDemo && (
          <span className="rounded-full bg-amber-100 px-3 py-1 text-xs font-medium text-amber-900">Demo data</span>
        )}
      </div>

      {isLoading && <p className="mt-6 text-slate-600">Loading risk assessments…</p>}
      {error && (
        <div role="alert" className="mt-6 rounded-lg border border-red-300 bg-red-50 p-4 text-red-900">
          Could not load risk data: {error.message}
        </div>
      )}

      {data && (
        <>
          <p className="mt-2 text-sm text-slate-600">{data.note}</p>

          <div className="mt-4 grid gap-4 sm:grid-cols-3">
            <div className="rounded-lg border border-slate-200 bg-white p-4">
              <div className="text-sm text-slate-600">High risk (score {data.bands.high}+)</div>
              <div className="mt-1 text-2xl font-semibold">{data.highCount}</div>
            </div>
            <div className="rounded-lg border border-slate-200 bg-white p-4">
              <div className="text-sm text-slate-600">Medium risk (score {data.bands.medium}–{data.bands.high - 1})</div>
              <div className="mt-1 text-2xl font-semibold">{data.mediumCount}</div>
            </div>
            <div className="rounded-lg border border-slate-200 bg-white p-4">
              <div className="text-sm text-slate-600">Estimated revenue exposure</div>
              <div className="mt-1 text-2xl font-semibold">{money(data.totalEstimatedExposure)}</div>
              <div className="mt-1 text-xs text-slate-500">Estimate over 3 months, each customer counted once</div>
            </div>
          </div>
          <p className="mt-2 text-xs text-slate-500">
            {data.scoredCustomers} customers scored; {data.notScoredCustomers} not scored because they have fewer than 3 paid orders.
          </p>

          <ul className="mt-4 space-y-4">
            {data.customers.map((c) => (
              <li key={c.id} className="rounded-lg border border-slate-200 bg-white p-4">
                <div className="flex flex-wrap items-center gap-3">
                  <h2 className="text-base font-semibold"><Link className="text-indigo-800 underline" to={`/app/customers/${c.id}`}>{c.name}</Link></h2>
                  <RiskBadge band={c.risk.band} score={c.risk.score} />
                  <span className="ml-auto text-sm text-slate-700">
                    Estimated exposure: <strong>{money(c.risk.estimatedExposure)}</strong>
                  </span>
                </div>
                <div className="mt-2 h-2 rounded-full bg-slate-200" aria-hidden="true">
                  <div className="h-2 rounded-full bg-indigo-700" style={{ width: `${c.risk.score}%` }} />
                </div>
                <h3 className="mt-3 text-sm font-medium text-slate-800">Evidence</h3>
                <ul className="mt-1 list-disc pl-5 text-sm text-slate-700">
                  {c.risk.evidence.map((e) => <li key={e}>{e}</li>)}
                </ul>
                <p className="mt-2 text-sm text-slate-600">
                  Last purchase {formatDate(c.lastPurchaseUtc, data.business.timeZoneId)} · usually buys every {c.risk.averageGapDays} days
                </p>
                <p className="mt-2 text-sm"><strong>Suggested action:</strong> {c.risk.suggestedAction}</p>
              </li>
            ))}
            {data.customers.length === 0 && <li className="text-slate-600">No customers are currently flagged.</li>}
          </ul>
        </>
      )}
    </div>
  )
}