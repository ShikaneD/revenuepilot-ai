import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { changeOpportunityStatus, fetchOpportunities } from '../lib/api'
import type { Opportunity } from '../lib/api'
import { formatDate, formatMoney } from '../lib/format'

const LABEL: Record<string, string> = {
  New: 'New', InProgress: 'In progress', Recovered: 'Recovered', Dismissed: 'Dismissed',
}
const STATUS_STYLE: Record<string, string> = {
  New: 'bg-slate-100 text-slate-800',
  InProgress: 'bg-indigo-100 text-indigo-900',
  Recovered: 'bg-emerald-100 text-emerald-900',
  Dismissed: 'bg-slate-200 text-slate-700',
}

function OpportunityCard({ o, currency, tz }: { o: Opportunity; currency: string; tz: string }) {
  const queryClient = useQueryClient()
  const [mode, setMode] = useState<null | 'recover' | 'dismiss'>(null)
  const [amount, setAmount] = useState('')
  const [note, setNote] = useState('')
  const money = (n: number) => formatMoney(n, currency)

  const mutation = useMutation({
    mutationFn: (body: { status: string; recoveredValue?: number; note?: string }) =>
      changeOpportunityStatus(o.id, body),
    onSuccess: () => {
      setMode(null)
      setAmount('')
      setNote('')
      queryClient.invalidateQueries({ queryKey: ['opportunities'] })
    },
  })

  const isOpen = o.status === 'New' || o.status === 'InProgress'
  const parsed = Number(amount)
  const amountValid = amount.trim() !== '' && Number.isFinite(parsed) && parsed > 0
  const field = 'rounded-md border border-slate-300 bg-white px-3 py-1.5 text-sm'
  const button = 'rounded-md border border-slate-300 bg-white px-3 py-1.5 text-sm text-slate-800 hover:bg-slate-100'

  return (
    <li className="rounded-lg border border-slate-200 bg-white p-4">
      <div className="flex flex-wrap items-center gap-3">
        <h2 className="text-base font-semibold">
          <Link className="text-indigo-800 underline" to={`/app/customers/${o.customerId}`}>{o.title}</Link>
        </h2>
        <span className={`rounded-full px-2.5 py-0.5 text-xs font-medium ${STATUS_STYLE[o.status]}`}>{LABEL[o.status]}</span>
        <span className="rounded-full bg-slate-100 px-2.5 py-0.5 text-xs font-medium text-slate-800">{o.priority} priority</span>
      </div>

      <dl className="mt-3 grid gap-3 text-sm sm:grid-cols-3">
        <div>
          <dt className="text-slate-600">Estimated value (forecast)</dt>
          <dd className="font-semibold">{money(o.estimatedValue)}</dd>
        </div>
        <div>
          <dt className="text-slate-600">Recovered (recorded by you)</dt>
          <dd className="font-semibold">{money(o.recoveredValue)}</dd>
        </div>
        <div>
          <dt className="text-slate-600">Created</dt>
          <dd className="font-semibold">{formatDate(o.createdAtUtc, tz)}</dd>
        </div>
      </dl>
      <p className="mt-2 text-sm text-slate-700"><strong>Why:</strong> {o.reason}</p>

      {isOpen && mode === null && (
        <div className="mt-3 flex flex-wrap gap-2">
          {o.status === 'New' && (
            <button className={button} disabled={mutation.isPending} onClick={() => mutation.mutate({ status: 'InProgress' })}>
              Start working on it
            </button>
          )}
          <button className={button} onClick={() => setMode('recover')}>Record recovered revenue</button>
          <button className={button} onClick={() => setMode('dismiss')}>Dismiss</button>
        </div>
      )}

      {isOpen && mode === 'recover' && (
        <form
          className="mt-3 space-y-2"
          onSubmit={(e) => {
            e.preventDefault()
            if (!amountValid) return
            mutation.mutate({
              status: 'Recovered',
              recoveredValue: Math.round(parsed * 100) / 100,
              note: note.trim() || undefined,
            })
          }}
        >
          <label className="block text-sm text-slate-800">
            Amount actually recovered ({currency})
            <input
              className={`${field} mt-1 block w-48`}
              type="number" min="0.01" step="0.01" inputMode="decimal"
              value={amount} onChange={(e) => setAmount(e.target.value)} required
            />
          </label>
          <label className="block text-sm text-slate-800">
            Note (optional)
            <input className={`${field} mt-1 block w-full max-w-md`} maxLength={500} value={note} onChange={(e) => setNote(e.target.value)} />
          </label>
          <p className="text-xs text-slate-600">Only record revenue you have confirmed, for example a payment received.</p>
          <div className="flex gap-2">
            <button type="submit" className="rounded-md bg-indigo-700 px-3 py-1.5 text-sm font-medium text-white disabled:opacity-50" disabled={!amountValid || mutation.isPending}>
              Save recovered amount
            </button>
            <button type="button" className={button} onClick={() => setMode(null)}>Cancel</button>
          </div>
        </form>
      )}

      {isOpen && mode === 'dismiss' && (
        <form
          className="mt-3 space-y-2"
          onSubmit={(e) => {
            e.preventDefault()
            mutation.mutate({ status: 'Dismissed', note: note.trim() || undefined })
          }}
        >
          <label className="block text-sm text-slate-800">
            Why are you dismissing it? (optional)
            <input className={`${field} mt-1 block w-full max-w-md`} maxLength={500} value={note} onChange={(e) => setNote(e.target.value)} />
          </label>
          <div className="flex gap-2">
            <button type="submit" className="rounded-md bg-slate-800 px-3 py-1.5 text-sm font-medium text-white disabled:opacity-50" disabled={mutation.isPending}>
              Confirm dismiss
            </button>
            <button type="button" className={button} onClick={() => setMode(null)}>Cancel</button>
          </div>
        </form>
      )}

      {mutation.error && (
        <div role="alert" className="mt-3 rounded-md border border-red-300 bg-red-50 p-2 text-sm text-red-900">
          {mutation.error.message}
        </div>
      )}

      <details className="mt-3 text-sm text-slate-700">
        <summary className="cursor-pointer font-medium">History ({o.activities.length})</summary>
        <ul className="mt-2 list-disc space-y-1 pl-5">
          {o.activities.map((a) => (
            <li key={`${a.createdAtUtc}-${a.toStatus}`}>
              {formatDate(a.createdAtUtc, tz)} ·{' '}
              {a.fromStatus ? `${LABEL[a.fromStatus] ?? a.fromStatus} → ${LABEL[a.toStatus] ?? a.toStatus}` : 'Created'}
              {a.recoveredValue !== null ? ` · recovered ${money(a.recoveredValue)}` : ''}
              {a.note ? ` · ${a.note}` : ''}
            </li>
          ))}
        </ul>
      </details>
    </li>
  )
}

export default function Opportunities() {
  const { data, isLoading, error } = useQuery({ queryKey: ['opportunities'], queryFn: fetchOpportunities })
  const money = (n: number) => formatMoney(n, data?.business.currency)

  return (
    <div className="mx-auto max-w-5xl">
      <div className="flex flex-wrap items-center gap-3">
        <h1 className="text-xl font-semibold">Revenue opportunities</h1>
        {data?.business.isDemo && (
          <span className="rounded-full bg-amber-100 px-3 py-1 text-xs font-medium text-amber-900">Demo data</span>
        )}
      </div>
      <p className="mt-2 text-sm text-slate-600">
        The estimated value is a forecast and is never counted as revenue. Recovered revenue counts only the amounts you record.
      </p>

      {isLoading && <p className="mt-6 text-slate-600">Loading opportunities…</p>}
      {error && (
        <div role="alert" className="mt-6 rounded-lg border border-red-300 bg-red-50 p-4 text-red-900">
          Could not load opportunities: {error.message}
        </div>
      )}

      {data && (
        <>
          <div className="mt-4 grid gap-4 sm:grid-cols-3">
            <div className="rounded-lg border border-slate-200 bg-white p-4">
              <div className="text-sm text-slate-600">Open, estimated</div>
              <div className="mt-1 text-2xl font-semibold">{money(data.openEstimatedValue)}</div>
              <div className="mt-1 text-xs text-slate-500">{data.openCount} open</div>
            </div>
            <div className="rounded-lg border border-slate-200 bg-white p-4">
              <div className="text-sm text-slate-600">Recovered, recorded by you</div>
              <div className="mt-1 text-2xl font-semibold">{money(data.recoveredValue)}</div>
              <div className="mt-1 text-xs text-slate-500">{data.recoveredCount} recovered</div>
            </div>
            <div className="rounded-lg border border-slate-200 bg-white p-4">
              <div className="text-sm text-slate-600">Dismissed</div>
              <div className="mt-1 text-2xl font-semibold">{data.dismissedCount}</div>
            </div>
          </div>

          <ul className="mt-4 space-y-4">
            {data.opportunities.map((o) => (
              <OpportunityCard key={o.id} o={o} currency={data.business.currency} tz={data.business.timeZoneId} />
            ))}
            {data.opportunities.length === 0 && (
              <li className="rounded-lg border border-slate-200 bg-white p-6 text-slate-700">
                No opportunities yet. Open <Link className="text-indigo-800 underline" to="/app/risk">Churn risk</Link> and
                create one from a flagged customer.
              </li>
            )}
          </ul>
        </>
      )}
    </div>
  )
}