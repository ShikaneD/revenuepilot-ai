import { useState } from 'react'
import { Link } from 'react-router-dom'
import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { fetchCustomers } from '../lib/api'
import { formatDate, formatMoney } from '../lib/format'
import RiskBadge from '../components/RiskBadge'

export default function Customers() {
  const [search, setSearch] = useState('')
  const [sort, setSort] = useState('revenue')
  const [risk, setRisk] = useState('')

  const { data, isLoading, error } = useQuery({
    queryKey: ['customers', search, sort, risk],
    queryFn: () => fetchCustomers({ search, sort, risk }),
    placeholderData: keepPreviousData,
  })

  const tz = data?.business.timeZoneId
  const field = 'rounded-md border border-slate-300 bg-white px-3 py-1.5 text-sm'

  return (
    <div className="mx-auto max-w-5xl">
      <div className="flex flex-wrap items-center gap-3">
        <h1 className="text-xl font-semibold">Customers</h1>
        {data?.business.isDemo && (
          <span className="rounded-full bg-amber-100 px-3 py-1 text-xs font-medium text-amber-900">Demo data</span>
        )}
      </div>

      <div className="mt-4 flex flex-wrap gap-3">
        <label className="text-sm text-slate-700">
          <span className="sr-only">Search customers</span>
          <input className={field} placeholder="Search by name" value={search} onChange={(e) => setSearch(e.target.value)} />
        </label>
        <label className="text-sm text-slate-700">
          <span className="sr-only">Sort by</span>
          <select className={field} value={sort} onChange={(e) => setSort(e.target.value)}>
            <option value="revenue">Sort: total revenue</option>
            <option value="risk">Sort: risk score</option>
            <option value="last">Sort: last purchase</option>
            <option value="name">Sort: name</option>
          </select>
        </label>
        <label className="text-sm text-slate-700">
          <span className="sr-only">Filter by risk</span>
          <select className={field} value={risk} onChange={(e) => setRisk(e.target.value)}>
            <option value="">All risk levels</option>
            <option value="high">High risk</option>
            <option value="medium">Medium risk</option>
            <option value="low">Low risk</option>
            <option value="none">Not scored</option>
          </select>
        </label>
      </div>

      {isLoading && <p className="mt-6 text-slate-600">Loading customers…</p>}
      {error && (
        <div role="alert" className="mt-6 rounded-lg border border-red-300 bg-red-50 p-4 text-red-900">
          Could not load customers: {error.message}
        </div>
      )}

      {data && (
        <div className="mt-4 overflow-x-auto rounded-lg border border-slate-200 bg-white">
          <table className="w-full text-left text-sm">
            <caption className="sr-only">Customers with revenue and churn-risk indicator</caption>
            <thead className="bg-slate-100 text-slate-700">
              <tr>
                <th scope="col" className="px-3 py-2">Customer</th>
                <th scope="col" className="px-3 py-2">Segment</th>
                <th scope="col" className="px-3 py-2">Last purchase</th>
                <th scope="col" className="px-3 py-2 text-right">Paid orders</th>
                <th scope="col" className="px-3 py-2 text-right">Total revenue</th>
                <th scope="col" className="px-3 py-2">Churn risk</th>
              </tr>
            </thead>
            <tbody>
              {data.customers.map((c) => (
                <tr key={c.id} className="border-t border-slate-200">
                  <td className="px-3 py-2 font-medium"><Link className="text-indigo-800 underline" to={`/app/customers/${c.id}`}>{c.name}</Link></td>
                  <td className="px-3 py-2">{c.segment ?? '—'}</td>
                  <td className="px-3 py-2">{formatDate(c.lastPurchaseUtc, tz)}</td>
                  <td className="px-3 py-2 text-right">{c.paidOrderCount}</td>
                  <td className="px-3 py-2 text-right">{formatMoney(c.totalRevenue, data.business.currency)}</td>
                  <td className="px-3 py-2">
                    {c.risk ? <RiskBadge band={c.risk.band} score={c.risk.score} /> : <span className="text-slate-500">Not enough data</span>}
                  </td>
                </tr>
              ))}
              {data.customers.length === 0 && (
                <tr><td colSpan={6} className="px-3 py-6 text-center text-slate-600">No customers match these filters.</td></tr>
              )}
            </tbody>
          </table>
        </div>
      )}
      {data && <p className="mt-2 text-xs text-slate-500">{data.count} customers shown. Customers with fewer than 3 paid orders are not scored.</p>}
    </div>
  )
}