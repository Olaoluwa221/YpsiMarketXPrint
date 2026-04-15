import { Link, useNavigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'

export default function Navbar() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()

  const handleLogout = () => {
    logout()
    navigate('/')
  }

  return (
    <nav className="bg-white border-b border-gray-200 px-6 py-4 flex items-center justify-between">
      <Link to="/" className="text-xl font-semibold text-gray-900">
        YpsiMarketXPrint
      </Link>

      <div className="flex items-center gap-6">
        {user?.userType === 'admin' ? (
          <>
            <Link to="/admin" className="text-sm text-gray-600 hover:text-gray-900">Dashboard</Link>
            <Link to="/admin/products" className="text-sm text-gray-600 hover:text-gray-900">Products</Link>
            <Link to="/admin/orders" className="text-sm text-gray-600 hover:text-gray-900">Orders</Link>
          </>
        ) : (
          <>
            <Link to="/products" className="text-sm text-gray-600 hover:text-gray-900">Shop</Link>
            <Link to="/cart" className="text-sm text-gray-600 hover:text-gray-900">Cart</Link>
            {user && <Link to="/profile" className="text-sm text-gray-600 hover:text-gray-900">Profile</Link>}
          </>
        )}

        {user ? (
          <button onClick={handleLogout} className="text-sm text-gray-600 hover:text-gray-900">
            Logout
          </button>
        ) : (
          <Link to="/login" className="text-sm bg-gray-900 text-white px-4 py-2 rounded-lg hover:bg-gray-700">
            Login
          </Link>
        )}
      </div>
    </nav>
  )
}