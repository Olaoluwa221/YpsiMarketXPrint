import { useState } from 'react'
import { Link, NavLink, useNavigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'

function NavItem({ to, children, onClick }) {
  return (
    <NavLink
      to={to}
      end
      onClick={onClick}
      className={({ isActive }) =>
        `text-sm px-3 py-2 rounded-lg border-b-2 transition-colors duration-200 ${
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
  const [menuOpen, setMenuOpen] = useState(false)

  const handleLogout = () => {
    logout()
    navigate('/')
    setMenuOpen(false)
  }

  const closeMenu = () => setMenuOpen(false)

  const navLinks = user?.userType === 'admin' ? (
    <>
      <NavItem to="/admin" onClick={closeMenu}>Dashboard</NavItem>
      <NavItem to="/admin/products" onClick={closeMenu}>Products</NavItem>
      <NavItem to="/admin/orders" onClick={closeMenu}>Orders</NavItem>
    </>
  ) : (
    <>
      <NavItem to="/products" onClick={closeMenu}>Shop</NavItem>
      <NavItem to="/cart" onClick={closeMenu}>Cart</NavItem>
      {user && <NavItem to="/profile" onClick={closeMenu}>Profile</NavItem>}
    </>
  )

  return (
    <nav style={{ backgroundColor: '#1B2A4A' }} className="px-6 py-4">
      <div className="flex items-center justify-between">
        {/* Logo */}
        <Link to="/" className="text-white font-bold text-lg tracking-wide shrink-0">
          Ypsi Marketing & Print
        </Link>

        {/* Desktop nav */}
        <div className="hidden md:flex items-center gap-4">
          {navLinks}
          {user ? (
            <button
              onClick={handleLogout}
              className="text-sm text-gray-300 hover:text-white px-3 py-2 rounded-lg border-b-2 border-transparent hover:border-orange-500 hover:bg-white/10 transition-colors duration-200"
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

        {/* Mobile hamburger */}
        <button
          className="md:hidden flex flex-col gap-1.5 p-2"
          onClick={() => setMenuOpen(!menuOpen)}
          aria-label="Toggle menu"
        >
          <span className={`block w-6 h-0.5 bg-white transition-all duration-200 ${menuOpen ? 'rotate-45 translate-y-2' : ''}`} />
          <span className={`block w-6 h-0.5 bg-white transition-all duration-200 ${menuOpen ? 'opacity-0' : ''}`} />
          <span className={`block w-6 h-0.5 bg-white transition-all duration-200 ${menuOpen ? '-rotate-45 -translate-y-2' : ''}`} />
        </button>
      </div>

      {/* Mobile menu */}
      {menuOpen && (
        <div className="md:hidden mt-4 flex flex-col gap-2 border-t border-white/10 pt-4">
          {navLinks}
          {user ? (
            <button
              onClick={handleLogout}
              className="text-sm text-gray-300 hover:text-white px-3 py-2 rounded-lg border-b-2 border-transparent hover:border-orange-500 hover:bg-white/10 transition-colors duration-200 text-left"
            >
              Logout
            </button>
          ) : (
            <Link
              to="/login"
              onClick={closeMenu}
              style={{ backgroundColor: '#E8620A' }}
              className="text-sm text-white px-5 py-2 rounded-lg hover:opacity-90 transition-opacity font-medium text-center"
            >
              Login
            </Link>
          )}
        </div>
      )}
    </nav>
  )
}