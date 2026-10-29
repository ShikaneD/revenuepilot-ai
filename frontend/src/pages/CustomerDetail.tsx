import { Link, useParams } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { fetchCustomer } from '../lib/api'
import { formatDate, formatMoney } from '../lib/format'
import RiskBadge from '../components/RiskBadge'

const monthLabel = (m: string) =>
  new Date(`${m}-01T00:00:00Z`).toLocaleDateString('en-ZA', { month: 'short', year: '2-digit', timeZone: 'UTC' })

const STATUS_STYLE: Record<string, string> = {
  Paid: 'bg-emerald-100 text-emerald-900',
  Failed: 'bg-red-100 text-red-900',
}

function Fact({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-lg border border-slate-200 bg-white p-4">
      <div className="text-sm text-slate-600">{label}</div>
      <div className="mt-1 text-xl font-semibold text-slate-900">{value}</div>
    </div>
  )
}

export default function CustomerDetail() {
  const { id = '' } = useParams()
  const { data, isLoading, error } = useQuery({
    queryKey: ['customer', id],
    queryFn: () => fetchCustomer(id),
  })

  const money = (n: number) => formatMoney(n, data?.business.currency)
  const tz = data?.business.timeZoneId
  const c = data?.customer
  const max = Math.max(1, ...(data?.monthlyRevenue.map((p) => p.revenue) ?? []))

  return (
    <div className="mx-auto max-w-5xl">
      <Link to="/app/customers" className="text-sm text-indigo-800 underline">← Back to customers</Link>

      {isLoading && <p className="mt-6 text-slate-600">Loading customer…</p>}
      {error && (
        <div role="alert" className="mt-6 rounded-lg border border-red-300 bg-red-50 p-4 text-red-900">
          Could not load this customer: {error.message}
        </div>
      )}

      {data && c && (
        <>
          <div className="mt-3 flex flex-wrap items-center gap-3">
            <h1 className="text-xl font-semibold">{c.name}</h1>
            {data.business.isDemo && (
              <span className="rounded-full bg-amber-100 px-3 py-1 text-xs font-medium text-amber-900">Demo data</span>
            )}
            {c.risk ? (
              <RiskBadge band={c.risk.band} score={c.risk.score} />
            ) : (
              <span className="text-sm text-slate-600">Not scored (needs at least 3 paid orders)</span>
            )}
          </div>
          <p className="mt-1 text-sm text-slate-600">{c.segment ?? 'No segment'} · {c.email ?? 'No email'}</p>

          <div className="mt-4 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
            <Fact label="Total revenue (net)" value={money(c.totalRevenue)} />
            <Fact label="Paid orders" value={String(c.paidOrderCount)} />
            <Fact label="Average order value" value={c.averageOrderValue === null ? 'No orders' : money(c.averageOrderValue)} />
            <Fact label="Last purchase" value={formatDate(c.lastPurchaseUtc, tz)} />
          </div>

          {c.risk && (
            <section className="mt-6 rounded-lg border border-slate-200 bg-white p-4" aria-labelledby="risk-heading">
              <h2 id="risk-heading" className="text-base font-semibold">Risk explanation</h2>
              <p className="mt-1 text-sm text-slate-600">
                A rule-based indicator, not a prediction. Usually buys every {c.risk.averageGapDays} days;{' '}
                {c.risk.daysSinceLastPurchase} days since the last paid order.
              </p>
              {c.risk.evidence.length > 0 ? (
                <ul className="mt-2 list-disc pl-5 text-sm text-slate-700">
                  {c.risk.evidence.map((e) => <li key={e}>{e}</li>)}
                </ul>
              ) : (
                <p className="mt-2 text-sm text-slate-700">No warning signs found in this customer’s order pattern.</p>
              )}
              <p className="mt-2 text-sm text-slate-700">
                Estimated exposure: <strong>{money(c.risk.estimatedExposure)}</strong> over 3 months, assuming the customer keeps buying at their historical pace.
              </p>
              <p className="mt-2 text-sm"><strong>Suggested action:</strong> {c.risk.suggestedAction}</p>
            </section>
          )}

          <section className="mt-6 rounded-lg border border-slate-200 bg-white p-4" aria-labelledby="trend-heading">
            <h2 id="trend-heading" className="text-base font-semibold">Net revenue by month</h2>
            <p className="text-xs text-slate-500">Last 12 months, paid orders minus refunds, months counted in {tz}.</p>
            <div className="mt-3 flex gap-1" role="img" aria-label="Bar chart of net revenue per month; the same figures are in the order table below">
              {data.monthlyRevenue.map((p) => (
                <div key={p.month} className="flex flex-1 flex-col items-center gap-1">
                  <div className="flex h-32 w-full items-end">
                    <div
                      className="w-full rounded-t bg-indigo-700"
                      style={{ height: `${(p.revenue / max) * 100}%` }}
                      title={`${monthLabel(p.month)}: ${money(p.revenue)}`}
                    />
                  </div>
                  <span className="text-[10px] text-slate-600">{monthLabel(p.month)}</span>
                </div>
              ))}
            </div>
          </section>

          <section className="mt-6" aria-labelledby="orders-heading">
            <h2 id="orders-heading" className="text-base font-semibold">Order history</h2>
            <div className="mt-2 overflow-x-auto rounded-lg border border-slate-200 bg-white">
              <table className="w-full text-left text-sm">
                <caption className="sr-only">All orders for {c.name}, newest first</caption>
                <thead className="bg-slate-100 text-slate-700">
                  <tr>
                    <th scope="col" className="px-3 py-2">Order</th>
                    <th scope="col" className="px-3 py-2">Date</th>
                    <th scope="col" className="px-3 py-2">Status</th>
                    <th scope="col" className="px-3 py-2 text-right">Total</th>
                    <th scope="col" className="px-3 py-2 text-right">Refund</th>
                    <th scope="col" className="px-3 py-2 text-right">Net revenue</th>
                  </tr>
                </thead>
                <tbody>
                  {data.orders.map((o) => (
                    <tr key={o.externalOrderId} className="border-t border-slate-200">
                      <td className="px-3 py-2">{o.externalOrderId}</td>
                      <td className="px-3 py-2">{formatDate(o.orderDateUtc, tz)}</td>
                      <td className="px-3 py-2">
                        <span className={`rounded-full px-2 py-0.5 text-xs font-medium ${STATUS_STYLE[o.status] ?? 'bg-slate-100 text-slate-800'}`}>
                          {o.status}
                        </span>
                      </td>
                      <td className="px-3 py-2 text-right">{money(o.totalAmount)}</td>
                      <td className="px-3 py-2 text-right">{money(o.refundAmount)}</td>
                      <td className="px-3 py-2 text-right">{money(o.netAmount)}</td>
                    </tr>
                  ))}
                  {data.orders.length === 0 && (
                    <tr><td colSpan={6} className="px-3 py-6 text-center text-slate-600">No orders yet.</td></tr>
                  )}
                </tbody>
              </table>
            </div>
          </section>
        </>
      )}
    </div>
  )
}