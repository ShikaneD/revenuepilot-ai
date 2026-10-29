export type RiskBand = 'Low' | 'Medium' | 'High'

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

export interface Risk {
  score: number
  band: RiskBand
  evidence: string[]
  estimatedExposure: number
  averageGapDays: number
  daysSinceLastPurchase: number
  suggestedAction: string
}

export interface CustomerInsight {
  id: string
  name: string
  segment: string | null
  email: string | null
  lastPurchaseUtc: string | null
  paidOrderCount: number
  totalRevenue: number
  averageOrderValue: number | null
  risk: Risk | null
}

interface BusinessInfo { name: string; currency: string; timeZoneId: string; isDemo: boolean }

export interface CustomersResponse { business: BusinessInfo; count: number; customers: CustomerInsight[] }

export interface RiskResponse {
  business: BusinessInfo
  scoredCustomers: number
  notScoredCustomers: number
  highCount: number
  mediumCount: number
  totalEstimatedExposure: number
  bands: { medium: number; high: number }
  note: string
  customers: (CustomerInsight & { risk: Risk })[]
}

async function getJson<T>(url: string): Promise<T> {
  const res = await fetch(url)
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
  return res.json() as Promise<T>
}

export const fetchSummary = (days: number) =>
  getJson<DashboardSummary>(`/api/v1/dashboard/summary?days=${days}`)

export function fetchCustomers(f: { search: string; sort: string; risk: string }) {
  const p = new URLSearchParams()
  if (f.search.trim()) p.set('search', f.search.trim())
  if (f.sort) p.set('sort', f.sort)
  if (f.risk) p.set('risk', f.risk)
  return getJson<CustomersResponse>(`/api/v1/customers?${p.toString()}`)
}

export const fetchRisk = () => getJson<RiskResponse>('/api/v1/risk')


export interface MonthlyPoint {
  month: string
  revenue: number
}

export interface OrderHistory {
  externalOrderId: string
  orderDateUtc: string
  status: string
  totalAmount: number
  refundAmount: number
  netAmount: number
}

export interface CustomerDetailResponse {
  business: BusinessInfo
  customer: CustomerInsight
  monthlyRevenue: MonthlyPoint[]
  orders: OrderHistory[]
}

export const fetchCustomer = (id: string) =>
  getJson<CustomerDetailResponse>(`/api/v1/customers/${id}`)