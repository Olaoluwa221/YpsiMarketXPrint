import { Link, NavLink, useNavigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'

function NavItem({ to, children }) {
  return (
    <NavLink
      to={to}
      end
      className={({ isActive }) =>
        `text-sm px-5 py-3 rounded-lg border-b-2 transition-colors duration-200 ${
          isActive
            ? 'text-white border-orange-500 bg-white/10'
            : 'text-gray-300 hover:text-white border-transparent hover:border-orange-500 hover:bg-white/10'
        }`
      }
    >
      {children}
    </NavLink>
  )
}

export default function Navbar() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()

  const handleLogout = () => {
    logout()
    navigate('/')
  }

  return (
    <nav style={{ backgroundColor: '#1B2A4A' }} className="px-8 py-4 flex items-center justify-between">
      <Link to="/" className="text-white font-bold text-lg tracking-wide">
        Ypsi Marketing & Print
      </Link>

      <div className="flex items-center gap-4">
        {user?.userType === 'admin' ? (
          <>
            <NavItem to="/admin">Dashboard</NavItem>
            <NavItem to="/admin/products">Products</NavItem>
            <NavItem to="/admin/orders">Orders</NavItem>
          </>
        ) : (
          <>
            <NavItem to="/products">Shop</NavItem>
            <NavItem to="/cart">Cart</NavItem>
            {user && <NavItem to="/profile">Profile</NavItem>}
          </>
        )}

        {user ? (
          <button
            onClick={handleLogout}
            className="text-sm text-gray-300 hover:text-white px-5 py-3 rounded-lg border-b-2 border-transparent hover:border-orange-500 hover:bg-white/10 transition-colors duration-200"
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