import { useState, useEffect } from 'react'
import { useParams, Link } from 'react-router-dom'
import api from '../api/axios'

export default function Unsubscribe() {
  const { token } = useParams()
  const [status, setStatus] = useState('loading') // 'loading' | 'success' | 'error'
  const [email, setEmail] = useState(null)
  const [message, setMessage] = useState('')

  useEffect(() => {
    const unsubscribe = async () => {
      try {
        const res = await api.post(`/Auth/unsubscribe/${token}`)
        setEmail(res.data?.email ?? null)
        setMessage(res.data?.message ?? 'You have been unsubscribed.')
        setStatus('success')
      } catch (err) {
        setMessage(err.response?.data?.message ?? 'This unsubscribe link is not valid.')
        setStatus('error')
      }
    }
    unsubscribe()
  }, [token])

  return (
    <div className="min-h-screen bg-gray-50 flex items-center justify-center px-4">
      <div className="bg-white rounded-2xl border border-gray-200 p-8 w-full max-w-md text-center">
        {status === 'loading' && (
          <p className="text-gray-400">Unsubscribing...</p>
        )}

        {status === 'success' && (
          <>
            <div
              className="w-16 h-16 rounded-full flex items-center justify-center text-3xl mx-auto mb-4 text-white"
              style={{ backgroundColor: '#10b981' }}
            >
              ✓
            </div>
            <h2 className="text-2xl font-bold mb-2" style={{ color: '#1B2A4A' }}>Unsubscribed</h2>
            <p className="text-gray-500 mb-2">{message}</p>
            {email && (
              <p className="text-sm text-gray-400 mb-6">
                <span className="font-semibold" style={{ color: '#1B2A4A' }}>{email}</span> will no longer receive marketing emails from us.
              </p>
            )}
            <p className="text-xs text-gray-400 mb-6">
              You'll still receive transactional emails (order confirmations, shipping updates, password resets).
            </p>
            <Link to="/" style={{ color: '#E8620A' }} className="text-sm font-medium hover:opacity-80">
              Back to home
            </Link>
          </>
        )}

        {status === 'error' && (
          <>
            <div
              className="w-16 h-16 rounded-full flex items-center justify-center text-3xl mx-auto mb-4 text-white"
              style={{ backgroundColor: '#f59e0b' }}
            >
              ⚠️
            </div>
            <h2 className="text-2xl font-bold mb-2" style={{ color: '#1B2A4A' }}>Link not valid</h2>
            <p className="text-gray-500 mb-6">{message}</p>
            <p className="text-xs text-gray-400 mb-6">
              You can also manage marketing preferences from your profile page if you have an account.
            </p>
            <Link to="/" style={{ color: '#E8620A' }} className="text-sm font-medium hover:opacity-80">
              Back to home
            </Link>
          </>
        )}
      </div>
    </div>
  )
}
