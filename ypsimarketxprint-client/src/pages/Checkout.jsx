import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import api from '../api/axios'
import { useToast } from '../context/ToastContext'

export default function Checkout() {
  const { showToast } = useToast()
  const navigate = useNavigate()
  const [cart, setCart] = useState(null)
  const [loading, setLoading] = useState(true)
  const [placing, setPlacing] = useState(false)
  const [showSuccess, setShowSuccess] = useState(false)
  const [confirmedOrderId, setConfirmedOrderId] = useState(null)
  const [deliveryMethod, setDeliveryMethod] = useState('shipping')
  const [form, setForm] = useState({
    firstName: '',
    lastName: '',
    email: '',
    phone: '',
    address: '',
    city: '',
    state: '',
    zip: '',
  })

  useEffect(() => {
    const fetchCart = async () => {
      try {
        const res = await api.get('/Cart')
        if (!res.data || res.data.items.length === 0) {
          navigate('/cart')
          return
        }
        setCart(res.data)
      } catch {
        navigate('/cart')
      } finally {
        setLoading(false)
      }
    }
    fetchCart()
  }, [])

  const handleSubmit = async (e) => {
    e.preventDefault()
    setPlacing(true)
    try {
      const res = await api.post('/Orders/checkout')
      setConfirmedOrderId(res.data.orderId)
      setShowSuccess(true)
    } catch {
      showToast('Failed to place order', 'error')
    } finally {
      setPlacing(false)
    }
  }

  if (loading) return (
    <div className="min-h-screen bg-gray-50 flex items-center justify-center">
      <div className="animate-pulse text-gray-400 text-lg">Loading...</div>
    </div>
  )

  return (
    <div className="min-h-screen bg-gray-50">
      <div className="max-w-5xl mx-auto px-6 py-12">

        <h1 className="text-3xl font-bold mb-8" style={{ color: '#1B2A4A' }}>Checkout</h1>

        <form onSubmit={handleSubmit}>
          <div className="flex flex-col lg:flex-row gap-8">

            {/* Left — form */}
            <div className="flex-1 space-y-6">

              {/* Delivery method */}
              <div className="bg-white rounded-2xl border border-gray-200 p-6">
                <h2 className="text-lg font-bold mb-4" style={{ color: '#1B2A4A' }}>Delivery method</h2>
                <div className="grid grid-cols-2 gap-4">
                  <button
                    type="button"
                    onClick={() => setDeliveryMethod('shipping')}
                    className={`p-4 rounded-xl border-2 text-left transition-all ${deliveryMethod === 'shipping'
                        ? 'border-orange-500 bg-orange-50'
                        : 'border-gray-200 hover:border-orange-300'
                      }`}
                  >
                    <div className="text-2xl mb-2">🚚</div>
                    <div className="font-semibold text-sm" style={{ color: '#1B2A4A' }}>Shipping</div>
                    <div className="text-xs text-gray-400 mt-0.5">Delivered to your door</div>
                  </button>
                  <button
                    type="button"
                    onClick={() => setDeliveryMethod('pickup')}
                    className={`p-4 rounded-xl border-2 text-left transition-all ${deliveryMethod === 'pickup'
                        ? 'border-orange-500 bg-orange-50'
                        : 'border-gray-200 hover:border-orange-300'
                      }`}
                  >
                    <div className="text-2xl mb-2">🏪</div>
                    <div className="font-semibold text-sm" style={{ color: '#1B2A4A' }}>Pickup</div>
                    <div className="text-xs text-gray-400 mt-0.5">Pick up in store</div>
                  </button>
                </div>
              </div>

              {/* Contact info */}
              <div className="bg-white rounded-2xl border border-gray-200 p-6">
                <h2 className="text-lg font-bold mb-4" style={{ color: '#1B2A4A' }}>Contact information</h2>
                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <label className="block text-sm font-medium mb-1" style={{ color: '#1B2A4A' }}>First name</label>
                    <input
                      type="text" required value={form.firstName}
                      onChange={e => setForm({ ...form, firstName: e.target.value })}
                      className="w-full border border-gray-300 rounded-lg px-4 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-orange-500"
                    />
                  </div>
                  <div>
                    <label className="block text-sm font-medium mb-1" style={{ color: '#1B2A4A' }}>Last name</label>
                    <input
                      type="text" required value={form.lastName}
                      onChange={e => setForm({ ...form, lastName: e.target.value })}
                      className="w-full border border-gray-300 rounded-lg px-4 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-orange-500"
                    />
                  </div>
                  <div>
                    <label className="block text-sm font-medium mb-1" style={{ color: '#1B2A4A' }}>Email</label>
                    <input
                      type="email" required value={form.email}
                      onChange={e => setForm({ ...form, email: e.target.value })}
                      className="w-full border border-gray-300 rounded-lg px-4 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-orange-500"
                    />
                  </div>
                  <div>
                    <label className="block text-sm font-medium mb-1" style={{ color: '#1B2A4A' }}>Phone</label>
                    <input
                      type="tel" value={form.phone}
                      onChange={e => setForm({ ...form, phone: e.target.value })}
                      className="w-full border border-gray-300 rounded-lg px-4 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-orange-500"
                    />
                  </div>
                </div>
              </div>

              {/* Shipping address */}
              {deliveryMethod === 'shipping' && (
                <div className="bg-white rounded-2xl border border-gray-200 p-6">
                  <h2 className="text-lg font-bold mb-4" style={{ color: '#1B2A4A' }}>Shipping address</h2>
                  <div className="space-y-4">
                    <div>
                      <label className="block text-sm font-medium mb-1" style={{ color: '#1B2A4A' }}>Street address</label>
                      <input
                        type="text" required value={form.address}
                        onChange={e => setForm({ ...form, address: e.target.value })}
                        className="w-full border border-gray-300 rounded-lg px-4 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-orange-500"
                        placeholder="123 Main St"
                      />
                    </div>
                    <div className="grid grid-cols-3 gap-4">
                      <div className="col-span-1">
                        <label className="block text-sm font-medium mb-1" style={{ color: '#1B2A4A' }}>City</label>
                        <input
                          type="text" required value={form.city}
                          onChange={e => setForm({ ...form, city: e.target.value })}
                          className="w-full border border-gray-300 rounded-lg px-4 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-orange-500"
                        />
                      </div>
                      <div>
                        <label className="block text-sm font-medium mb-1" style={{ color: '#1B2A4A' }}>State</label>
                        <input
                          type="text" required value={form.state}
                          onChange={e => setForm({ ...form, state: e.target.value })}
                          className="w-full border border-gray-300 rounded-lg px-4 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-orange-500"
                          placeholder="MI"
                        />
                      </div>
                      <div>
                        <label className="block text-sm font-medium mb-1" style={{ color: '#1B2A4A' }}>ZIP</label>
                        <input
                          type="text" required value={form.zip}
                          onChange={e => setForm({ ...form, zip: e.target.value })}
                          className="w-full border border-gray-300 rounded-lg px-4 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-orange-500"
                        />
                      </div>
                    </div>
                  </div>
                </div>
              )}

              {/* Pickup info */}
              {deliveryMethod === 'pickup' && (
                <div className="bg-orange-50 border border-orange-200 rounded-2xl p-6">
                  <h2 className="text-lg font-bold mb-2" style={{ color: '#1B2A4A' }}>Pickup location</h2>
                  <p className="text-gray-600 text-sm">Ypsi Marketing & Print Company</p>
                  <p className="text-gray-500 text-sm mt-1">Ypsilanti, Michigan</p>
                  <p className="text-gray-500 text-sm mt-3">You'll receive a confirmation when your order is ready for pickup.</p>
                </div>
              )}

              {/* Payment placeholder */}
              <div className="bg-white rounded-2xl border border-gray-200 p-6">
                <h2 className="text-lg font-bold mb-4" style={{ color: '#1B2A4A' }}>Payment</h2>
                <div className="bg-gray-50 border border-gray-200 rounded-xl p-4 text-center text-sm text-gray-400">
                  Stripe payment coming soon — orders will be confirmed manually for now.
                </div>
              </div>

            </div>

            {/* Right — summary */}
            <div className="lg:w-80 shrink-0">
              <div className="bg-white rounded-2xl border border-gray-200 p-6 sticky top-6">
                <h2 className="text-lg font-bold mb-6" style={{ color: '#1B2A4A' }}>Order summary</h2>

                {cart && (
                  <>
                    <div className="space-y-3 mb-6">
                      {cart.items.map(item => (
                        <div key={item.variantId} className="flex justify-between text-sm">
                          <span className="text-gray-500 truncate mr-2">
                            {item.productName} ({item.size}) x{item.quantity}
                          </span>
                          <span className="font-medium shrink-0" style={{ color: '#1B2A4A' }}>
                            ${item.subtotal.toFixed(2)}
                          </span>
                        </div>
                      ))}
                    </div>

                    <div className="border-t border-gray-100 pt-4 mb-6">
                      <div className="flex justify-between">
                        <span className="font-bold" style={{ color: '#1B2A4A' }}>Total</span>
                        <span className="font-bold text-xl" style={{ color: '#E8620A' }}>
                          ${cart.total.toFixed(2)}
                        </span>
                      </div>
                    </div>
                  </>
                )}

                <button
                  type="submit"
                  disabled={placing}
                  style={{ backgroundColor: '#E8620A' }}
                  className="w-full py-4 rounded-xl text-white font-semibold hover:opacity-90 disabled:opacity-50 transition-opacity shadow-lg"
                >
                  {placing ? 'Placing order...' : 'Place order'}
                </button>
              </div>
            </div>
          </div>
        </form>
      </div>

      {/* Success Modal */}
      {showSuccess && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
          <div className="bg-white rounded-2xl p-8 w-full max-w-md shadow-2xl text-center">
            <div
              className="w-20 h-20 rounded-full flex items-center justify-center text-4xl mx-auto mb-4 shadow-lg text-white"
              style={{ backgroundColor: '#E8620A' }}
            >
              ✓
            </div>
            <h2 className="text-2xl font-bold mb-2" style={{ color: '#1B2A4A' }}>Order confirmed!</h2>
            <p className="text-gray-500 mb-2">Thank you for your order.</p>
            <p className="text-sm text-gray-400 mb-8">
              Order <span className="font-semibold" style={{ color: '#1B2A4A' }}>#{confirmedOrderId}</span> has been placed and we'll get started right away.
            </p>
            <div className="flex flex-col gap-3">
              <button
                onClick={() => navigate('/profile')}
                className="w-full py-3 rounded-xl text-sm font-semibold border-2 transition-colors"
                style={{ color: '#1B2A4A', borderColor: '#1B2A4A' }}
              >
                View order history
              </button>
              <button
                onClick={() => navigate('/')}
                style={{ backgroundColor: '#E8620A' }}
                className="w-full py-3 rounded-xl text-sm font-semibold text-white hover:opacity-90 transition-opacity"
              >
                Back to home
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}