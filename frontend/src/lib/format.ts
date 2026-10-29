export const formatMoney = (n: number, currency = 'ZAR') =>
  new Intl.NumberFormat('en-ZA', { style: 'currency', currency }).format(n)

export const formatDate = (iso: string | null, timeZone = 'Africa/Johannesburg') =>
  iso
    ? new Date(iso).toLocaleDateString('en-ZA', { timeZone, day: 'numeric', month: 'short', year: 'numeric' })
    : '—'