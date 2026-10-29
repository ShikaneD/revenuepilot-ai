import { Link, NavLink, Outlet } from 'react-router-dom'

const LINKS = [
  { to: '/app', label: 'Overview', end: true },
  { to: '/app/customers', label: 'Customers', end: false },
  { to: '/app/risk', label: 'Churn risk', end: false },
]

export default function AppLayout() {
  return (
    <div className="min-h-screen bg-slate-50 text-slate-900 md:flex">
      <aside className="bg-slate-900 p-4 text-slate-100 md:min-h-screen md:w-56 md:shrink-0">
        <Link to="/" className="block px-2 text-lg font-semibold text-white">RevenuePilot AI</Link>
        <nav aria-label="Main" className="mt-4 flex gap-1 overflow-x-auto md:flex-col">
          {LINKS.map((l) => (
            <NavLink
              key={l.to}
              to={l.to}
              end={l.end}
              className={({ isActive }) =>
                `whitespace-nowrap rounded-md px-3 py-2 text-sm ${
                  isActive ? 'bg-slate-700 text-white' : 'text-slate-300 hover:bg-slate-800'
                }`
              }
            >
              {l.label}
            </NavLink>
          ))}
        </nav>
        <p className="mt-6 hidden px-2 text-xs text-slate-400 md:block">
          More pages arrive in later phases: opportunities, AI analyst, reports and imports.
        </p>
      </aside>
      <main className="min-w-0 flex-1 px-4 py-6 md:px-8">
        <Outlet />
      </main>
    </div>
  )
}