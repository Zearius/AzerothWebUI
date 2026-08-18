import { Navigate, Route, Routes } from 'react-router'
import { AdminAuthProvider } from './AdminAuthContext'
import { PlayerAuthProvider } from './PlayerAuthContext'
import Register from './pages/Register'
import PlayerLogin from './pages/PlayerLogin'
import AdminLogin from './pages/AdminLogin'
import AdminLayout from './pages/AdminLayout'
import AdminStatus from './pages/AdminStatus'
import AdminAccounts from './pages/AdminAccounts'
import AdminConfig from './pages/AdminConfig'
import AdminAhBot from './pages/AdminAhBot'
import AdminAwardItem from './pages/AdminAwardItem'
import ArmoryCharacters from './pages/ArmoryCharacters'
import ArmoryCharacterDetail from './pages/ArmoryCharacterDetail'
import ArmoryItemSearch from './pages/ArmoryItemSearch'
import ArmoryItemDetail from './pages/ArmoryItemDetail'
import './App.css'

function App() {
  return (
    <AdminAuthProvider>
      <PlayerAuthProvider>
        <Routes>
          <Route path="/" element={<Register />} />
          <Route path="/login" element={<PlayerLogin />} />
          <Route path="/armory" element={<Navigate to="/armory/characters" replace />} />
          <Route path="/armory/characters" element={<ArmoryCharacters />} />
          <Route path="/armory/characters/:name" element={<ArmoryCharacterDetail />} />
          <Route path="/armory/items" element={<ArmoryItemSearch />} />
          <Route path="/armory/items/:id" element={<ArmoryItemDetail />} />
          <Route path="/admin/login" element={<AdminLogin />} />
          <Route path="/admin" element={<AdminLayout />}>
            <Route index element={<Navigate to="status" replace />} />
            <Route path="status" element={<AdminStatus />} />
            <Route path="accounts" element={<AdminAccounts />} />
            <Route path="config" element={<AdminConfig />} />
            <Route path="ahbot" element={<AdminAhBot />} />
            <Route path="award-item" element={<AdminAwardItem />} />
          </Route>
        </Routes>
      </PlayerAuthProvider>
    </AdminAuthProvider>
  )
}

export default App
