import { useState, useEffect } from 'react'
import { Link } from 'react-router-dom'
import api from '../../api/axios'

export default function AdminDashboard() {
  const [orders, setOrders] = useState([])
  const [products, setProducts] = useState([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    const fetchData = async () => {
      try {
        const [ordersRes, productsRes] = await Promise.all([
          api.get('/Orders/all'),
          api.get('/Products')
        ])
        setOrders(ordersRes.data)
        setProducts(productsRes.data)
      } catch (err) {
        console.error('Failed to load dashboard data', err)
      } finally {
        setLoading(false)
      }
    }
    fetchData()
  }, [])

  const statusColors = {
    pending: 'bg-yellow-50 text-yellow-600 border-yellow-200',
    processing: 'bg-blue-50 text-blue-600 border-blue-200',
    shipped: 'bg-purple-50 text-purple-600 border-purple-200',
    delivered: 'bg-green-50 text-green-600 border-green-200',
    cancelled: 'bg-red-50 text-red-600 border-red-200',
  }

  const totalRevenue = orders
    .filter(o => o.orderStatus !== 'cancelled')
    .reduce((sum, o) => sum + o.total, 0)

  const pendingOrders = orders.filter(o => o.orderStatus === 'pending').length
  const recentOrders = [...orders]
    .sort((a, b) => new Date(b.dateOrdered) - new Date(a.dateOrdered))
    .slice(0, 5)

  const stats = [
    { label: 'Total orders', value: orders.length, emoji: '📋' },
    { label: 'Pending orders', value: pendingOrders, emoji: '⏳' },
    { label: 'Total products', value: products.length, emoji: '📦' },
    { label: 'Total revenue', value: `$${totalRevenue.toFixed(2)}`, emoji: '💰' },
  ]

  return (
    <div className="min-h-screen bg-gray-50 p-6">
      <div className="max-w-7xl mx-auto">

        {/* Header */}
        <div className="mb-8">
          <h1 className="text-3xl font-bold" style={{ color: '#1B2A4A' }}>Dashboard</h1>
          <p className="text-gray-500 mt-1">Welcome back — here's what's happening</p>
        </div>

        {/* Stats */}
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-6 mb-10">
          {stats.map(stat => (
            <div key={stat.label} className="bg-white rounded-2xl border border-gray-200 p-6 hover:border-orange-300 hover:shadow-md transition-all">
              <div className="text-3xl mb-3">{stat.emoji}</div>
              <div className="text-2xl font-bold mb-1" style={{ color: '#1B2A4A' }}>
                {loading ? '—' : stat.value}
              </div>
              <div className="text-sm text-gray-400">{stat.label}</div>
            </div>
          ))}
        </div>

        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">

          {/* Recent orders */}
          <div className="lg:col-span-2 bg-white rounded-2xl border border-gray-200 overflow-hidden">
            <div className="flex items-center justify-between px-6 py-4 border-b border-gray-100">
              <h2 className="font-bold" style={{ color: '#1B2A4A' }}>Recent orders</h2>
              <Link to="/admin/orders" className="text-sm hover:opacity-80 transition-opacity font-medium" style={{ color: '#E8620A' }}>
                View all →
              </Link>
            </div>

            {loading ? (
              <div className="p-8 text-center text-gray-400">Loading...</div>
            ) : recentOrders.length === 0 ? (
              <div className="p-12 text-center">
                <div className="text-4xl mb-3">📋</div>
                <p className="text-gray-400 text-sm">No orders yet</p>
              </div>
            ) : (
              <table className="w-full">
                <thead>
                  <tr style={{ backgroundColor: '#f8f9fa' }}>
                    <th className="text-left px-6 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wide">Order</th>
                    <th className="text-left px-6 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wide">Date</th>
                    <th className="text-left px-6 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wide">Total</th>
                    <th className="text-left px-6 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wide">Status</th>
                  </tr>
                </thead>
                <tbody>
                  {recentOrders.map((order, i) => (
                    <tr
                      key={order.orderId}
                      className={`border-t border-gray-100 hover:bg-gray-50 transition-colors ${i === recentOrders.length - 1 ? 'border-0' : ''}`}
                    >
                      <td className="px-6 py-4 text-sm font-medium" style={{ color: '#1B2A4A' }}>#{order.orderId}</td>
                      <td className="px-6 py-4 text-sm text-gray-500">{new Date(order.dateOrdered).toLocaleDateString()}</td>
                      <td className="px-6 py-4 text-sm font-semibold" style={{ color: '#E8620A' }}>${order.total.toFixed(2)}</td>
                      <td className="px-6 py-4">
                        <span className={`text-xs font-medium px-2.5 py-1 rounded-full border capitalize ${statusColors[order.orderStatus]}`}>
                          {order.orderStatus}
                        </span>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </div>

          {/* Quick actions */}
          <div className="space-y-4">
            <div className="bg-white rounded-2xl border border-gray-200 p-6">
              <h2 className="font-bold mb-4" style={{ color: '#1B2A4A' }}>Quick actions</h2>
              <div className="space-y-3">
                <Link
                  to="/admin/products"
                  className="flex items-center gap-3 p-3 rounded-xl border border-gray-200 hover:border-orange-400 hover:bg-orange-50 transition-all group"
                >
                  <span className="text-2xl">📦</span>
                  <div>
                    <p className="text-sm font-medium group-hover:text-orange-500 transition-colors" style={{ color: '#1B2A4A' }}>Manage products</p>
                    <p className="text-xs text-gray-400">Add, edit or remove products</p>
                  </div>
                </Link>
                <Link
                  to="/admin/orders"
                  className="flex items-center gap-3 p-3 rounded-xl border border-gray-200 hover:border-orange-400 hover:bg-orange-50 transition-all group"
                >
                  <span className="text-2xl">📋</span>
                  <div>
                    <p className="text-sm font-medium group-hover:text-orange-500 transition-colors" style={{ color: '#1B2A4A' }}>Manage orders</p>
                    <p className="text-xs text-gray-400">View and update order status</p>
                  </div>
                </Link>
                <Link
                  to="/products"
                  className="flex items-center gap-3 p-3 rounded-xl border border-gray-200 hover:border-orange-400 hover:bg-orange-50 transition-all group"
                >
                  <span className="text-2xl">🛍️</span>
                  <div>
                    <p className="text-sm font-medium group-hover:text-orange-500 transition-colors" style={{ color: '#1B2A4A' }}>View storefront</p>
                    <p className="text-xs text-gray-400">See the customer-facing site</p>
                  </div>
                </Link>
              </div>
            </div>

            {/* Order breakdown */}
            <div className="bg-white rounded-2xl border border-gray-200 p-6">
              <h2 className="font-bold mb-4" style={{ color: '#1B2A4A' }}>Order breakdown</h2>
              {loading ? (
                <div className="text-gray-400 text-sm text-center py-4">Loading...</div>
              ) : (
                <div className="space-y-2">
                  {['pending', 'processing', 'shipped', 'delivered', 'cancelled'].map(status => {
                    const count = orders.filter(o => o.orderStatus === status).length
                    const pct = orders.length > 0 ? (count / orders.length) * 100 : 0
                    return (
                      <div key={status}>
                        <div className="flex justify-between text-xs mb-1">
                          <span className="capitalize text-gray-500">{status}</span>
                          <span className="font-medium" style={{ color: '#1B2A4A' }}>{count}</span>
                        </div>
                        <div className="w-full bg-gray-100 rounded-full h-1.5">
                          <div
                            className="h-1.5 rounded-full transition-all duration-500"
                            style={{ width: `${pct}%`, backgroundColor: '#E8620A' }}
                          />
                        </div>
                      </div>
                    )
                  })}
                </div>
              )}
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}