import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { fetchSummary } from './lib/api'

const PERIODS = [7, 30, 90] as const

function Kpi({ label, value, note }: { label: string; value: string; note?: string }) {
  return (
    <div className="rounded-lg border border-slate-200 bg-white p-4">
      <div className="text-sm text-slate-600">{label}</div>
      <div className="mt-1 text-2xl font-semibold text-slate-900">{value}</div>
      {note && <div className="mt-1 text-xs text-slate-500">{note}</div>}
    </div>
  )
}

export default function App() {
  const [days, setDays] = useState<number>(30)
  const { data, isLoading, error } = useQuery({
    queryKey: ['summary', days],
    queryFn: () => fetchSummary(days),
  })

  const money = (n: number) =>
    new Intl.NumberFormat('en-ZA', { style: 'currency', currency: data?.business.currency ?? 'ZAR' }).format(n)

  // Period ends are exclusive, so show the last included moment.
  const day = (iso: string, endExclusive = false) =>
    new Date(new Date(iso).getTime() - (endExclusive ? 1 : 0)).toLocaleDateString('en-ZA', {
      timeZone: data?.business.timeZoneId,
      day: 'numeric', month: 'short', year: 'numeric',
    })

  return (
    <div className="min-h-screen bg-slate-50 text-slate-900">
      <header className="bg-slate-900 px-6 py-4 text-white">
        <span className="text-lg font-semibold">RevenuePilot AI</span>
      </header>

      <main className="mx-auto max-w-5xl px-6 py-6">
        <div className="flex flex-wrap items-center gap-3">
          <h1 className="text-xl font-semibold">Overview</h1>
          {data?.business.isDemo && (
            <span className="rounded-full bg-amber-100 px-3 py-1 text-xs font-medium text-amber-900">Demo data</span>
          )}
          <div className="ml-auto flex gap-2" role="group" aria-label="Reporting period">
            {PERIODS.map((p) => (
              <button
                key={p}
                onClick={() => setDays(p)}
                aria-pressed={days === p}
                className={`rounded-md border px-3 py-1.5 text-sm ${
                  days === p ? 'border-indigo-700 bg-indigo-700 text-white' : 'border-slate-300 bg-white text-slate-800 hover:bg-slate-100'
                }`}
              >
                Last {p} days
              </button>
            ))}
          </div>
        </div>

        {isLoading && <p className="mt-6 text-slate-600">Loading figures…</p>}

        {error && (
          <div role="alert" className="mt-6 rounded-lg border border-red-300 bg-red-50 p-4 text-red-900">
            Could not load the dashboard: {error.message}. Check that the API is running on port 5175.
          </div>
        )}

        {data && (
          <>
            <p className="mt-2 text-sm text-slate-600">
              {data.business.name} · {day(data.period.startUtc)} to {day(data.period.endUtc, true)} · compared with{' '}
              {day(data.comparisonPeriod.startUtc)} to {day(data.comparisonPeriod.endUtc, true)}
            </p>

            <div className="mt-4 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
              <Kpi label="Net revenue" value={money(data.netRevenue)} note={`Previous period: ${money(data.previousNetRevenue)}`} />
              <Kpi
                label="Growth vs previous period"
                value={data.growthPercent === null ? 'Not applicable' : `${data.growthPercent > 0 ? '+' : ''}${data.growthPercent}%`}
                note={data.growthPercent === null ? 'No revenue in the previous period' : data.growthPercent < 0 ? 'Decline' : 'Growth'}
              />
              <Kpi label="Paid orders" value={String(data.orderCount)} />
              <Kpi label="Average order value" value={data.averageOrderValue === null ? 'No orders' : money(data.averageOrderValue)} />
              <Kpi label="Refunds" value={money(data.refundsTotal)} />
              <Kpi label="Failed payments" value={String(data.failedPaymentCount)} />
            </div>

            <details className="mt-6 text-sm text-slate-700">
              <summary className="cursor-pointer font-medium">How these figures are calculated</summary>
              <ul className="mt-2 list-disc space-y-1 pl-5">
                <li>{data.definitions.netRevenue}</li>
                <li>{data.definitions.growthPercent}</li>
              </ul>
            </details>
          </>
        )}
      </main>
    </div>
  )
}