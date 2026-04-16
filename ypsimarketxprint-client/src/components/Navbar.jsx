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
    <nav style={{ backgroundColor: '#1B2A4A' }} className="px-8 py-4 flex items-center justify-between">
      <Link to="/" className="flex items-center gap-3">
        <span className="text-white font-bold text-lg tracking-wide">
          Ypsi Marketing & Print
        </span>
      </Link>

      <div className="flex items-center gap-8">
        {user?.userType === 'admin' ? (
          <>
            <Link to="/admin" className="text-sm text-gray-300 hover:text-white px-1 py-2 relative after:absolute after:bottom-0 after:left-0 after:h-0.5 after:w-0 hover:after:w-full after:bg-orange-500 after:transition-all after:duration-300">Dashboard</Link>
            <Link to="/admin/products" className="text-sm text-gray-300 hover:text-white px-1 py-2 relative after:absolute after:bottom-0 after:left-0 after:h-0.5 after:w-0 hover:after:w-full after:bg-orange-500 after:transition-all after:duration-300">Products</Link>
            <Link to="/admin/orders" className="text-sm text-gray-300 hover:text-white px-1 py-2 relative after:absolute after:bottom-0 after:left-0 after:h-0.5 after:w-0 hover:after:w-full after:bg-orange-500 after:transition-all after:duration-300">Orders</Link>
          </>
        ) : (
          <>
            <Link to="/products" className="text-sm text-gray-300 hover:text-white px-1 py-2 relative after:absolute after:bottom-0 after:left-0 after:h-0.5 after:w-0 hover:after:w-full after:bg-orange-500 after:transition-all after:duration-300">Shop</Link>
            <Link to="/cart" className="text-sm text-gray-300 hover:text-white px-1 py-2 relative after:absolute after:bottom-0 after:left-0 after:h-0.5 after:w-0 hover:after:w-full after:bg-orange-500 after:transition-all after:duration-300">Cart</Link>
            {user && <Link to="/profile" className="text-sm text-gray-300 hover:text-white px-1 py-2 relative after:absolute after:bottom-0 after:left-0 after:h-0.5 after:w-0 hover:after:w-full after:bg-orange-500 after:transition-all after:duration-300">Profile</Link>}
          </>
        )}

        {user ? (
          <button
            onClick={handleLogout}
            className="text-sm text-gray-300 hover:text-white px-1 py-2 relative after:absolute after:bottom-0 after:left-0 after:h-0.5 after:w-0 hover:after:w-full after:bg-orange-500 after:transition-all after:duration-300"
          >
            Logout
          </button>
        ) : (
          <Link
            to="/login"
            style={{ backgroundColor: '#E8620A' }}
            className="text-sm text-white px-5 py-2 rounded-lg hover:opacity-90 transition-opacity font-medium"
          >
            Login
          </Link>
        )}
      </div>
    </nav>
  )
}