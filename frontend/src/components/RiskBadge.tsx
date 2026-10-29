import type { RiskBand } from '../lib/api'

const STYLES: Record<RiskBand, string> = {
  Low: 'bg-emerald-100 text-emerald-900',
  Medium: 'bg-amber-100 text-amber-900',
  High: 'bg-red-100 text-red-900',
}

// The band is always written out, so colour is never the only signal.
export default function RiskBadge({ band, score }: { band: RiskBand; score?: number }) {
  return (
    <span className={`inline-block rounded-full px-2.5 py-0.5 text-xs font-medium ${STYLES[band]}`}>
      {band} risk{score !== undefined ? ` · ${score}` : ''}
    </span>
  )
}