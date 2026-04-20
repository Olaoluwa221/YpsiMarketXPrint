import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import api from '../api/axios'
import { useAuth } from '../context/AuthContext'
import { useToast } from '../context/ToastContext'
import { SkeletonRow } from '../components/Skeleton'

const statusColors = {
  pending: 'bg-yellow-50 text-yellow-600 border-yellow-200',
  processing: 'bg-blue-50 text-blue-600 border-blue-200',
  shipped: 'bg-purple-50 text-purple-600 border-purple-200',
  delivered: 'bg-green-50 text-green-600 border-green-200',
  cancelled: 'bg-red-50 text-red-600 border-red-200',
}

export default function Profile() {
  const { user, logout } = useAuth()
  const { showToast } = useToast()
  const navigate = useNavigate()
  const [orders, setOrders] = useState([])
  const [loading, setLoading] = useState(true)
  const [expandedOrder, setExpandedOrder] = useState(null)
  const [editingName, setEditingName] = useState(false)
  const [editingPassword, setEditingPassword] = useState(false)
  const [nameForm, setNameForm] = useState({ firstName: '', lastName: '' })
  const [passwordForm, setPasswordForm] = useState({ currentPassword: '', newPassword: '', confirmPassword: '' })
  const [savingName, setSavingName] = useState(false)
  const [savingPassword, setSavingPassword] = useState(false)

  useEffect(() => {
    const fetchOrders = async () => {
      try {
        const res = await api.get('/Orders')
        setOrders(res.data)
      } catch {
        showToast('Failed to load orders', 'error')
      } finally {
        setLoading(false)
      }
    }
    fetchOrders()
  }, [])

  const handleLogout = () => {
    logout()
    navigate('/')
  }

  const [marketingOptIn, setMarketingOptIn] = useState(false)
  const [updatingOptIn, setUpdatingOptIn] = useState(false)

  useEffect(() => {
    const fetchMe = async () => {
      try {
        const res = await api.get('/Auth/me')
        setMarketingOptIn(res.data.marketingOptIn)
        setNameForm({ firstName: res.data.firstName || '', lastName: res.data.lastName || '' })
      } catch {
        console.error('Failed to fetch user details')
      }
    }
    fetchMe()
  }, [])

  const handleSaveName = async () => {
    setSavingName(true)
    try {
      await api.put('/Auth/update-profile', nameForm)
      showToast('Name updated!')
      setEditingName(false)
    } catch {
      showToast('Failed to update name', 'error')
    } finally {
      setSavingName(false)
    }
  }

  const handleSavePassword = async () => {
    if (passwordForm.newPassword !== passwordForm.confirmPassword) {
      showToast('Passwords do not match', 'error')
      return
    }
    if (passwordForm.newPassword.length < 8) {
      showToast('Password must be at least 8 characters', 'error')
      return
    }
    setSavingPassword(true)
    try {
      await api.put('/Auth/update-password', {
        currentPassword: passwordForm.currentPassword,
        newPassword: passwordForm.newPassword
      })
      showToast('Password updated!')
      setEditingPassword(false)
      setPasswordForm({ currentPassword: '', newPassword: '', confirmPassword: '' })
    } catch {
      showToast('Current password is incorrect', 'error')
    } finally {
      setSavingPassword(false)
    }
  }

  const handleOptInToggle = async () => {
    setUpdatingOptIn(true)
    try {
      const res = await api.put('/Auth/marketing-opt-in', !marketingOptIn, {
        headers: { 'Content-Type': 'application/json' }
      })
      setMarketingOptIn(res.data.marketingOptIn)
      showToast(res.data.marketingOptIn ? 'Subscribed to marketing emails' : 'Unsubscribed from marketing emails')
    } catch {
      showToast('Failed to update preference', 'error')
    } finally {
      setUpdatingOptIn(false)
    }
  }
  return (
    <div className="min-h-screen bg-gray-50">
      <div className="max-w-4xl mx-auto px-6 py-12">

        {/* Header */}
        <div className="flex items-center justify-between mb-10">
          <div>
            <h1 className="text-3xl font-bold" style={{ color: '#1B2A4A' }}>My profile</h1>
            <p className="text-gray-500 mt-1">{user?.email}</p>
          </div>
          <button
            onClick={handleLogout}
            className="px-4 py-2 rounded-xl text-sm font-medium border-2 border-gray-300 text-gray-600 hover:border-red-400 hover:text-red-500 transition-all"
          >
            Logout
          </button>
        </div>

        {/* Account info card */}
        <div className="bg-white rounded-2xl border border-gray-200 p-6 mb-8">
          <div className="flex items-center justify-between mb-4">
            <h2 className="text-lg font-bold" style={{ color: '#1B2A4A' }}>Account details</h2>
            {!editingName && !editingPassword && (
              <button
                onClick={() => setEditingName(true)}
                className="text-sm font-medium hover:opacity-80 transition-opacity"
                style={{ color: '#E8620A' }}
              >
                Edit name
              </button>
            )}
          </div>

          {editingName ? (
            <div className="space-y-4">
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium mb-1" style={{ color: '#1B2A4A' }}>First name</label>
                  <input
                    type="text" value={nameForm.firstName}
                    onChange={e => setNameForm({ ...nameForm, firstName: e.target.value })}
                    className="w-full border border-gray-300 rounded-lg px-4 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-orange-500"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium mb-1" style={{ color: '#1B2A4A' }}>Last name</label>
                  <input
                    type="text" value={nameForm.lastName}
                    onChange={e => setNameForm({ ...nameForm, lastName: e.target.value })}
                    className="w-full border border-gray-300 rounded-lg px-4 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-orange-500"
                  />
                </div>
              </div>
              <div className="flex gap-3">
                <button
                  onClick={handleSaveName}
                  disabled={savingName}
                  style={{ backgroundColor: '#E8620A' }}
                  className="px-5 py-2 rounded-xl text-sm font-semibold text-white hover:opacity-90 disabled:opacity-50 transition-opacity"
                >
                  {savingName ? 'Saving...' : 'Save'}
                </button>
                <button
                  onClick={() => setEditingName(false)}
                  className="px-5 py-2 rounded-xl text-sm font-medium border border-gray-200 text-gray-600 hover:border-gray-400 transition-colors"
                >
                  Cancel
                </button>
              </div>
            </div>
          ) : (
            <div className="grid grid-cols-2 gap-4 mb-4">
              <div>
                <p className="text-xs text-gray-400 mb-1">First name</p>
                <p className="text-sm font-medium" style={{ color: '#1B2A4A' }}>{nameForm.firstName || '—'}</p>
              </div>
              <div>
                <p className="text-xs text-gray-400 mb-1">Last name</p>
                <p className="text-sm font-medium" style={{ color: '#1B2A4A' }}>{nameForm.lastName || '—'}</p>
              </div>
              <div>
                <p className="text-xs text-gray-400 mb-1">Email</p>
                <p className="text-sm font-medium" style={{ color: '#1B2A4A' }}>{user?.email}</p>
              </div>
              <div>
                <p className="text-xs text-gray-400 mb-1">Account type</p>
                <p className="text-sm font-medium capitalize" style={{ color: '#1B2A4A' }}>{user?.userType}</p>
              </div>
            </div>
          )}

          {/* Change password */}
          {!editingName && (
            <div className="border-t border-gray-100 pt-4 mt-4">
              {editingPassword ? (
                <div className="space-y-4">
                  <h3 className="text-sm font-semibold" style={{ color: '#1B2A4A' }}>Change password</h3>
                  <div>
                    <label className="block text-sm font-medium mb-1" style={{ color: '#1B2A4A' }}>Current password</label>
                    <input
                      type="password" value={passwordForm.currentPassword}
                      onChange={e => setPasswordForm({ ...passwordForm, currentPassword: e.target.value })}
                      className="w-full border border-gray-300 rounded-lg px-4 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-orange-500"
                    />
                  </div>
                  <div>
                    <label className="block text-sm font-medium mb-1" style={{ color: '#1B2A4A' }}>New password</label>
                    <input
                      type="password" value={passwordForm.newPassword}
                      onChange={e => setPasswordForm({ ...passwordForm, newPassword: e.target.value })}
                      className="w-full border border-gray-300 rounded-lg px-4 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-orange-500"
                    />
                  </div>
                  <div>
                    <label className="block text-sm font-medium mb-1" style={{ color: '#1B2A4A' }}>Confirm new password</label>
                    <input
                      type="password" value={passwordForm.confirmPassword}
                      onChange={e => setPasswordForm({ ...passwordForm, confirmPassword: e.target.value })}
                      className="w-full border border-gray-300 rounded-lg px-4 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-orange-500"
                    />
                  </div>
                  <div className="flex gap-3">
                    <button
                      onClick={handleSavePassword}
                      disabled={savingPassword}
                      style={{ backgroundColor: '#E8620A' }}
                      className="px-5 py-2 rounded-xl text-sm font-semibold text-white hover:opacity-90 disabled:opacity-50 transition-opacity"
                    >
                      {savingPassword ? 'Saving...' : 'Update password'}
                    </button>
                    <button
                      onClick={() => {
                        setEditingPassword(false)
                        setPasswordForm({ currentPassword: '', newPassword: '', confirmPassword: '' })
                      }}
                      className="px-5 py-2 rounded-xl text-sm font-medium border border-gray-200 text-gray-600 hover:border-gray-400 transition-colors"
                    >
                      Cancel
                    </button>
                  </div>
                </div>
              ) : (
                <button
                  onClick={() => setEditingPassword(true)}
                  className="text-sm font-medium hover:opacity-80 transition-opacity"
                  style={{ color: '#E8620A' }}
                >
                  Change password
                </button>
              )}
            </div>
          )}
        </div>

        {/* Marketing preferences */}
        {/* Email preferences */}
        <div className="bg-white rounded-2xl border border-gray-200 p-6 mb-8">
          <h2 className="text-lg font-bold mb-4" style={{ color: '#1B2A4A' }}>Email preferences</h2>
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium" style={{ color: '#1B2A4A' }}>Marketing emails</p>
              <p className="text-xs text-gray-400 mt-0.5">Receive updates about new products and special offers</p>
            </div>
            <button
              onClick={handleOptInToggle}
              disabled={updatingOptIn}
              style={{
                position: 'relative',
                width: '52px',
                height: '28px',
                borderRadius: '14px',
                backgroundColor: marketingOptIn ? '#E8620A' : '#D1D5DB',
                border: 'none',
                cursor: updatingOptIn ? 'not-allowed' : 'pointer',
                transition: 'background-color 0.25s ease',
                padding: 0,
                flexShrink: 0,
                opacity: updatingOptIn ? 0.6 : 1,
              }}
              aria-label="Toggle marketing emails"
            >
              <span style={{
                position: 'absolute',
                top: '4px',
                left: marketingOptIn ? '28px' : '4px',
                width: '20px',
                height: '20px',
                backgroundColor: 'white',
                borderRadius: '50%',
                boxShadow: '0 1px 4px rgba(0,0,0,0.25)',
                transition: 'left 0.25s ease',
                display: 'block',
              }} />
            </button>
          </div>
        </div>

        {/* Order history */}
        <div>
          <h2 className="text-xl font-bold mb-4" style={{ color: '#1B2A4A' }}>Order history</h2>

          {loading ? (
            <div className="space-y-3">
              {Array.from({ length: 3 }).map((_, i) => <SkeletonRow key={i} />)}
            </div>
          ) : orders.length === 0 ? (
            <div className="bg-white rounded-2xl border border-gray-200 p-16 text-center">
              <div className="text-5xl mb-4">📋</div>
              <h3 className="text-lg font-semibold mb-2" style={{ color: '#1B2A4A' }}>No orders yet</h3>
              <p className="text-gray-500 mb-6">Place your first order to see it here</p>
              <button
                onClick={() => navigate('/products')}
                style={{ backgroundColor: '#E8620A' }}
                className="px-6 py-2.5 rounded-xl text-sm font-medium text-white hover:opacity-90"
              >
                Browse products
              </button>
            </div>
          ) : (
            <div className="space-y-4">
              {orders.map(order => (
                <div key={order.orderId} className="bg-white rounded-2xl border border-gray-200 overflow-hidden">
                  {/* Order header */}
                  <button
                    onClick={() => setExpandedOrder(expandedOrder === order.orderId ? null : order.orderId)}
                    className="w-full flex items-center justify-between p-5 hover:bg-gray-50 transition-colors"
                  >
                    <div className="flex items-center gap-4">
                      <div className="text-left">
                        <p className="font-bold text-sm" style={{ color: '#1B2A4A' }}>Order #{order.orderId}</p>
                        <p className="text-xs text-gray-400 mt-0.5">{new Date(order.dateOrdered).toLocaleDateString()}</p>
                      </div>
                      <span className={`text-xs font-medium px-2.5 py-1 rounded-full border capitalize ${statusColors[order.orderStatus]}`}>
                        {order.orderStatus}
                      </span>
                    </div>
                    <div className="flex items-center gap-4">
                      <span className="font-bold" style={{ color: '#E8620A' }}>${order.total.toFixed(2)}</span>
                      <span className="text-gray-400 text-sm">{expandedOrder === order.orderId ? '▲' : '▼'}</span>
                    </div>
                  </button>

                  {/* Order items */}
                  {expandedOrder === order.orderId && (
                    <div className="border-t border-gray-100 p-5 space-y-3">
                      {order.items.map((item, i) => (
                        <div key={i} className="flex justify-between text-sm">
                          <div>
                            <p className="font-medium" style={{ color: '#1B2A4A' }}>{item.productName}</p>
                            <p className="text-gray-400 text-xs mt-0.5">{item.size} × {item.quantity}</p>
                          </div>
                          <span className="font-medium" style={{ color: '#1B2A4A' }}>${item.subtotal.toFixed(2)}</span>
                        </div>
                      ))}
                      <div className="border-t border-gray-100 pt-3 flex justify-between">
                        <span className="font-bold text-sm" style={{ color: '#1B2A4A' }}>Total</span>
                        <span className="font-bold text-sm" style={{ color: '#E8620A' }}>${order.total.toFixed(2)}</span>
                      </div>
                    </div>
                  )}
                </div>
              ))}
            </div>
          )}
        </div>
      </div>
    </div>
  )
}