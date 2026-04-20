import { useState } from 'react'
import { Link } from 'react-router-dom'
import api from '../api/axios'

export default function ForgotPassword() {
  const [email, setEmail] = useState('')
  const [loading, setLoading] = useState(false)
  const [sent, setSent] = useState(false)

  const handleSubmit = async (e) => {
    e.preventDefault()
    setLoading(true)
    try {
      await api.post('/Auth/forgot-password', { email })
      setSent(true)
    } catch {
      setSent(true) // Still show success to prevent email enumeration
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="min-h-screen bg-gray-50 flex items-center justify-center px-4">
      <div className="bg-white rounded-2xl border border-gray-200 p-8 w-full max-w-md shadow-sm">

        {sent ? (
          <div className="text-center">
            <div className="text-5xl mb-4">📧</div>
            <h2 className="text-2xl font-bold mb-2" style={{ color: '#1B2A4A' }}>Check your email</h2>
            <p className="text-gray-500 mb-6">
              If an account exists for <strong>{email}</strong>, we've sent a password reset link. Check your inbox.
            </p>
            <Link to="/login" className="text-sm font-medium hover:opacity-80 transition-opacity" style={{ color: '#E8620A' }}>
              Back to login
            </Link>
          </div>
        ) : (
          <>
            <h1 className="text-2xl font-bold mb-2" style={{ color: '#1B2A4A' }}>Forgot password</h1>
            <p className="text-gray-500 text-sm mb-6">Enter your email and we'll send you a reset link.</p>

            <form onSubmit={handleSubmit} className="space-y-4">
              <div>
                <label className="block text-sm font-medium mb-1" style={{ color: '#1B2A4A' }}>Email</label>
                <input
                  type="email" required value={email}
                  onChange={e => setEmail(e.target.value)}
                  placeholder="you@example.com"
                  className="w-full border border-gray-300 rounded-lg px-4 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-orange-500"
                />
              </div>

              <button
                type="submit"
                disabled={loading}
                style={{ backgroundColor: '#E8620A' }}
                className="w-full py-3 rounded-xl text-white font-semibold hover:opacity-90 disabled:opacity-50 transition-opacity"
              >
                {loading ? 'Sending...' : 'Send reset link'}
              </button>
            </form>

            <p className="text-center text-sm text-gray-400 mt-6">
              Remember your password?{' '}
              <Link to="/login" className="font-medium hover:opacity-80 transition-opacity" style={{ color: '#E8620A' }}>
                Sign in
              </Link>
            </p>
          </>
        )}
      </div>
    </div>
  )
}