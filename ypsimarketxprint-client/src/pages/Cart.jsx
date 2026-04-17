import { useState, useEffect } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import api from '../api/axios'
import { useAuth } from '../context/AuthContext'
import { useToast } from '../context/ToastContext'
import { SkeletonRow } from '../components/Skeleton'

export default function Cart() {
  const { user } = useAuth()
  const { showToast } = useToast()
  const navigate = useNavigate()
  const [cart, setCart] = useState(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    if (!user) {
      setLoading(false)
      return
    }
    fetchCart()
  }, [user])

  const fetchCart = async () => {
    try {
      const res = await api.get('/Cart')
      setCart(res.data)
    } catch {
      showToast('Failed to load cart', 'error')
    } finally {
      setLoading(false)
    }
  }

  const handleUpdateQuantity = async (variantId, quantity) => {
    try {
      await api.put(`/Cart/items/${variantId}`, { quantity })
      fetchCart()
    } catch {
      showToast('Failed to update quantity', 'error')
    }
  }

  const handleRemove = async (variantId) => {
    try {
      await api.delete(`/Cart/items/${variantId}`)
      showToast('Item removed')
      fetchCart()
    } catch {
      showToast('Failed to remove item', 'error')
    }
  }

  const handleClear = async () => {
    if (!confirm('Clear your entire cart?')) return
    try {
      await api.delete('/Cart')
      showToast('Cart cleared')
      fetchCart()
    } catch {
      showToast('Failed to clear cart', 'error')
    }
  }

  if (!user) return (
    <div className="min-h-screen bg-gray-50 flex flex-col items-center justify-center gap-4">
      <div className="text-6xl">🛒</div>
      <h2 className="text-2xl font-bold" style={{ color: '#1B2A4A' }}>Your cart is waiting</h2>
      <p className="text-gray-500">Sign in to view your cart</p>
      <Link to="/login" style={{ backgroundColor: '#E8620A' }} className="px-8 py-3 rounded-xl text-white font-semibold hover:opacity-90 transition-opacity">
        Sign in
      </Link>
    </div>
  )

  return (
    <div className="min-h-screen bg-gray-50">
      <div className="max-w-5xl mx-auto px-6 py-12">

        <h1 className="text-3xl font-bold mb-8" style={{ color: '#1B2A4A' }}>Your cart</h1>

        {loading ? (
          <div className="space-y-3">
            {Array.from({ length: 3 }).map((_, i) => <SkeletonRow key={i} />)}
          </div>
        ) : !cart || cart.items.length === 0 ? (
          <div className="text-center py-24">
            <div className="text-7xl mb-6">🛒</div>
            <h2 className="text-2xl font-bold mb-3" style={{ color: '#1B2A4A' }}>Your cart is empty</h2>
            <p className="text-gray-500 mb-8">Add some products to get started</p>
            <Link to="/products" style={{ backgroundColor: '#E8620A' }} className="px-8 py-3 rounded-xl text-white font-semibold hover:opacity-90 transition-opacity">
              Browse products
            </Link>
          </div>
        ) : (
          <div className="flex flex-col lg:flex-row gap-8">

            {/* Items */}
            <div className="flex-1 space-y-4">
              {cart.items.map(item => (
                <div key={item.variantId} className="bg-white rounded-2xl border border-gray-200 p-5 flex gap-4 items-center hover:border-orange-200 transition-colors">
                  {/* Image */}
                  <div className="w-20 h-20 rounded-xl overflow-hidden border border-gray-100 shrink-0 bg-gray-50 flex items-center justify-center">
                    {item.imageLink ? (
                      <img src={item.imageLink} alt={item.productName} className="w-full h-full object-cover" />
                    ) : (
                      <span className="text-2xl">🖨️</span>
                    )}
                  </div>

                  {/* Info */}
                  <div className="flex-1 min-w-0">
                    <h3 className="font-semibold text-sm" style={{ color: '#1B2A4A' }}>{item.productName}</h3>
                    <p className="text-gray-400 text-xs mt-0.5">{item.size}</p>
                    <p className="text-sm font-bold mt-1" style={{ color: '#E8620A' }}>${item.price.toFixed(2)} each</p>
                  </div>

                  {/* Quantity */}
                  <div className="flex items-center gap-2">
                    <button
                      onClick={() => handleUpdateQuantity(item.variantId, item.quantity - 1)}
                      className="w-8 h-8 rounded-lg border border-gray-200 flex items-center justify-center text-gray-500 hover:border-orange-400 hover:text-orange-500 transition-all font-bold"
                    >
                      −
                    </button>
                    <span className="w-8 text-center font-semibold text-sm" style={{ color: '#1B2A4A' }}>{item.quantity}</span>
                    <button
                      onClick={() => handleUpdateQuantity(item.variantId, item.quantity + 1)}
                      className="w-8 h-8 rounded-lg border border-gray-200 flex items-center justify-center text-gray-500 hover:border-orange-400 hover:text-orange-500 transition-all font-bold"
                    >
                      +
                    </button>
                  </div>

                  {/* Subtotal */}
                  <div className="text-right shrink-0">
                    <p className="font-bold text-sm" style={{ color: '#1B2A4A' }}>${item.subtotal.toFixed(2)}</p>
                    <button
                      onClick={() => handleRemove(item.variantId)}
                      className="text-xs text-red-400 hover:text-red-600 transition-colors mt-1"
                    >
                      Remove
                    </button>
                  </div>
                </div>
              ))}

              <button
                onClick={handleClear}
                className="text-sm text-gray-400 hover:text-red-500 transition-colors mt-2"
              >
                Clear cart
              </button>
            </div>

            {/* Summary */}
            <div className="lg:w-80 shrink-0">
              <div className="bg-white rounded-2xl border border-gray-200 p-6 sticky top-6">
                <h2 className="text-lg font-bold mb-6" style={{ color: '#1B2A4A' }}>Order summary</h2>

                <div className="space-y-3 mb-6">
                  {cart.items.map(item => (
                    <div key={item.variantId} className="flex justify-between text-sm">
                      <span className="text-gray-500 truncate mr-2">{item.productName} ({item.size}) x{item.quantity}</span>
                      <span className="font-medium shrink-0" style={{ color: '#1B2A4A' }}>${item.subtotal.toFixed(2)}</span>
                    </div>
                  ))}
                </div>

                <div className="border-t border-gray-100 pt-4 mb-6">
                  <div className="flex justify-between">
                    <span className="font-bold" style={{ color: '#1B2A4A' }}>Total</span>
                    <span className="font-bold text-xl" style={{ color: '#E8620A' }}>${cart.total.toFixed(2)}</span>
                  </div>
                </div>

                <button
                  onClick={() => navigate('/checkout')}
                  style={{ backgroundColor: '#E8620A' }}
                  className="w-full py-4 rounded-xl text-white font-semibold hover:opacity-90 transition-opacity shadow-lg"
                >
                  Proceed to checkout
                </button>

                <Link
                  to="/products"
                  className="block text-center text-sm text-gray-400 hover:text-gray-600 transition-colors mt-4"
                >
                  Continue shopping
                </Link>
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  )
}