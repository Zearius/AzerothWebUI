import { Navigate, Route, Routes } from 'react-router'
import { AdminAuthProvider } from './AdminAuthContext'
import Register from './pages/Register'
import AdminLogin from './pages/AdminLogin'
import AdminLayout from './pages/AdminLayout'
import AdminStatus from './pages/AdminStatus'
import AdminAccounts from './pages/AdminAccounts'
import AdminConfig from './pages/AdminConfig'
import './App.css'

function App() {
  return (
    <AdminAuthProvider>
      <Routes>
        <Route path="/" element={<Register />} />
        <Route path="/admin/login" element={<AdminLogin />} />
        <Route path="/admin" element={<AdminLayout />}>
          <Route index element={<Navigate to="status" replace />} />
          <Route path="status" element={<AdminStatus />} />
          <Route path="accounts" element={<AdminAccounts />} />
          <Route path="config" element={<AdminConfig />} />
        </Route>
      </Routes>
    </AdminAuthProvider>
  )
}

export default App
