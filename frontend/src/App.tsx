import { Navigate, Route, Routes } from 'react-router-dom'
import AppLayout from './components/AppLayout'
import Landing from './pages/Landing'
import Overview from './pages/Overview'
import Customers from './pages/Customers'
import CustomerDetail from './pages/CustomerDetail'
import RiskPage from './pages/RiskPage'
import Opportunities from './pages/Opportunities'

export default function App() {
  return (
    <Routes>
      <Route path="/" element={<Landing />} />
      <Route path="/app" element={<AppLayout />}>
        <Route index element={<Overview />} />
        <Route path="customers" element={<Customers />} />
        <Route path="customers/:id" element={<CustomerDetail />} />
        <Route path="risk" element={<RiskPage />} />
        <Route path="opportunities" element={<Opportunities />} />
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}