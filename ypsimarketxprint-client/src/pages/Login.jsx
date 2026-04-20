import { useState, useEffect } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'
import { useToast } from '../context/ToastContext'
import api from '../api/axios'

export default function Login() {
  const { login } = useAuth()
  const { showToast } = useToast()
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const [form, setForm] = useState({ email: '', password: '' })
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

  useEffect(() => {
    if (searchParams.get('reset') === 'success') {
      showToast('Password reset successfully! Please log in.')
    }
  }, [])

  const handleSubmit = async (e) => {
    e.preventDefault()
    setError('')
    setLoading(true)
    try {
      const res = await api.post('/Auth/login', form)
      login(res.data)
      if (res.data.userType === 'admin') {
        navigate('/admin')
      } else {
        navigate('/')
      }
    } catch {
      setError('Invalid email or password.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="min-h-screen flex">
      {/* Left panel */}
      <div style={{ backgroundColor: '#1B2A4A' }} className="hidden lg:flex lg:w-1/2 flex-col justify-between p-12">
        <span className="text-white font-bold text-xl tracking-wide">
          Ypsi Marketing & Print
        </span>
        <div>
          <h2 className="text-4xl font-bold text-white leading-tight mb-4">
            Professional printing,<br />made simple.
          </h2>
          <p className="text-blue-200 text-lg">
            Order yard signs, banners, business cards, and more — all in one place.
          </p>
        </div>
        <p className="text-blue-300 text-sm">© 2026 Ypsi Marketing & Print Company</p>
      </div>

      {/* Right panel */}
      <div className="flex-1 flex items-center justify-center px-8 bg-gray-50">
        <div className="w-full max-w-md">
          <div className="lg:hidden mb-8">
            <span className="text-white font-bold text-xl tracking-wide">
              Ypsi Marketing & Print
            </span>
          </div>

          <h1 className="text-3xl font-bold mb-2" style={{ color: '#1B2A4A' }}>Welcome back</h1>
          <p className="text-gray-500 mb-8">Sign in to your account to continue</p>

          {error && (
            <div className="bg-red-50 border border-red-200 text-red-700 text-sm rounded-lg px-4 py-3 mb-6">
              {error}
            </div>
          )}

          <form onSubmit={handleSubmit} className="space-y-5">
            <div>
              <label className="block text-sm font-medium mb-1" style={{ color: '#1B2A4A' }}>
                Email address
              </label>
              <input
                type="email" required value={form.email}
                onChange={e => setForm({ ...form, email: e.target.value })}
                className="w-full border border-gray-300 rounded-lg px-4 py-3 text-sm focus:outline-none focus:ring-2 focus:ring-orange-500"
                placeholder="you@example.com"
              />
            </div>
            <div>
              <label className="block text-sm font-medium mb-1" style={{ color: '#1B2A4A' }}>
                Password
              </label>
              <input
                type="password" required value={form.password}
                onChange={e => setForm({ ...form, password: e.target.value })}
                className="w-full border border-gray-300 rounded-lg px-4 py-3 text-sm focus:outline-none focus:ring-2 focus:ring-orange-500"
                placeholder="••••••••"
              />
            </div>
            <div className="text-right">
              <Link to="/forgot-password" className="text-xs hover:opacity-80 transition-opacity" style={{ color: '#E8620A' }}>
                Forgot password?
              </Link>
            </div>
            <button
              type="submit"
              disabled={loading}
              style={{ backgroundColor: '#E8620A' }}
              className="w-full text-white py-3 rounded-lg text-sm font-semibold hover:opacity-90 disabled:opacity-50 transition-opacity"
            >
              {loading ? 'Signing in...' : 'Sign in'}
            </button>
          </form>

          <p className="text-sm text-gray-500 mt-6 text-center">
            Don't have an account?{' '}
            <Link to="/register" className="font-semibold hover:opacity-80 transition-opacity" style={{ color: '#E8620A' }}>
              Sign up
            </Link>
          </p>
        </div>
      </div>
    </div>
  )
} 