export interface DashboardSummary {
  business: { name: string; currency: string; timeZoneId: string; isDemo: boolean }
  period: { days: number; startUtc: string; endUtc: string }
  comparisonPeriod: { startUtc: string; endUtc: string }
  netRevenue: number
  previousNetRevenue: number
  growthPercent: number | null
  orderCount: number
  averageOrderValue: number | null
  refundsTotal: number
  failedPaymentCount: number
  definitions: { netRevenue: string; growthPercent: string }
}

export async function fetchSummary(days: number): Promise<DashboardSummary> {
  const res = await fetch(`/api/v1/dashboard/summary?days=${days}`)
  if (!res.ok) {
    let message = `Request failed (${res.status})`
    try {
      const body = await res.json()
      if (body?.error) message = body.error
    } catch {
      /* response had no JSON body */
    }
    throw new Error(message)
  }
  return res.json()
}