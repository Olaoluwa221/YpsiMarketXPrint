import { useState, useEffect } from 'react'
import api from '../../api/axios'
import { useToast } from '../../context/ToastContext'

const statusColors = {
  pending: 'bg-yellow-50 text-yellow-600 border-yellow-200',
  processing: 'bg-blue-50 text-blue-600 border-blue-200',
  shipped: 'bg-purple-50 text-purple-600 border-purple-200',
  delivered: 'bg-green-50 text-green-600 border-green-200',
  readyforpickup: 'bg-orange-50 text-orange-600 border-orange-200',
  pickedup: 'bg-green-50 text-green-600 border-green-200',
  cancelled: 'bg-red-50 text-red-600 border-red-200',
}

const allStatuses = ['pending', 'processing', 'shipped', 'delivered', 'readyforpickup', 'pickedup', 'cancelled']
const shippingStatuses = ['pending', 'processing', 'shipped', 'delivered', 'cancelled']
const pickupStatuses = ['pending', 'processing', 'readyforpickup', 'pickedup', 'cancelled']

const getStatuses = (deliveryMethod) =>
  deliveryMethod === 'pickup' ? pickupStatuses : shippingStatuses

const formatStatus = (status) =>
  status.replace('readyforpickup', 'Ready for Pickup').replace('pickedup', 'Picked Up')

export default function AdminOrders() {
  const { showToast } = useToast()
  const [orders, setOrders] = useState([])
  const [loading, setLoading] = useState(true)
  const [selectedOrder, setSelectedOrder] = useState(null)
  const [filterStatus, setFilterStatus] = useState('all')

  useEffect(() => { fetchOrders() }, [])

  const fetchOrders = async () => {
    try {
      const res = await api.get('/Orders/all')
      setOrders(res.data)
    } catch {
      showToast('Failed to load orders', 'error')
    } finally {
      setLoading(false)
    }
  }

  const handleViewArtwork = async (orderId, variantId) => {
    try {
      const res = await api.get(`/Images/orders/${orderId}/artwork/${variantId}/url`)
      window.open(res.data.url, '_blank')
    } catch {
      showToast('Failed to load artwork', 'error')
    }
  }

  const handleRegenerateLink = async (orderId, variantId) => {
    if (!confirm('Send a new artwork upload link to the customer? Any previous link will stop working.')) return
    try {
      await api.post(`/Orders/${orderId}/regenerate-artwork-token/${variantId}`)
      showToast('New upload link emailed to customer')
    } catch {
      showToast('Failed to send new link', 'error')
    }
  }

  const handleStatusUpdate = async (orderId, newStatus) => {
    try {
      await api.put(`/Orders/${orderId}/status`, { orderStatus: newStatus })
      showToast('Order status updated')
      fetchOrders()
      if (selectedOrder?.orderId === orderId) {
        setSelectedOrder(prev => ({ ...prev, orderStatus: newStatus }))
      }
    } catch {
      showToast('Failed to update status', 'error')
    }
  }

  const filtered = filterStatus === 'all'
    ? orders
    : orders.filter(o => o.orderStatus === filterStatus)

  return (
    <div className="min-h-screen bg-gray-50 p-6">
      <div className="max-w-7xl mx-auto">

        {/* Header */}
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 mb-8">
          <div>
            <h1 className="text-3xl font-bold" style={{ color: '#1B2A4A' }}>Orders</h1>
            <p className="text-gray-500 mt-1">{orders.length} total orders</p>
          </div>
        </div>

        {/* Status filters */}
        <div className="flex gap-2 flex-wrap mb-6">
          {['all', ...allStatuses].map(status => (
            <button
              key={status}
              onClick={() => setFilterStatus(status)}
              className={`px-4 py-2 rounded-full text-sm font-medium capitalize transition-all duration-200 ${filterStatus === status
                  ? 'text-white shadow-md scale-105'
                  : 'bg-white text-gray-600 border border-gray-200 hover:border-orange-400 hover:text-orange-500'
                }`}
              style={filterStatus === status ? { backgroundColor: '#E8620A' } : {}}
            >
              {formatStatus(status)}
            </button>
          ))}
        </div>

        <div className="flex gap-6">
          {/* Orders list */}
          <div className="flex-1 bg-white rounded-2xl border border-gray-200 overflow-hidden">
            {loading ? (
              <div className="p-8 text-center text-gray-400">Loading...</div>
            ) : filtered.length === 0 ? (
              <div className="p-16 text-center">
                <div className="text-5xl mb-4">📋</div>
                <h3 className="text-lg font-semibold mb-2" style={{ color: '#1B2A4A' }}>No orders found</h3>
                <p className="text-gray-500">No orders match this filter</p>
              </div>
            ) : (
              <table className="w-full">
                <thead>
                  <tr style={{ backgroundColor: '#f8f9fa' }} className="border-b border-gray-200">
                    <th className="text-left px-6 py-4 text-xs font-semibold text-gray-500 uppercase tracking-wide">Order</th>
                    <th className="text-left px-6 py-4 text-xs font-semibold text-gray-500 uppercase tracking-wide">Date</th>
                    <th className="text-left px-6 py-4 text-xs font-semibold text-gray-500 uppercase tracking-wide">Delivery</th>
                    <th className="text-left px-6 py-4 text-xs font-semibold text-gray-500 uppercase tracking-wide">Total</th>
                    <th className="text-left px-6 py-4 text-xs font-semibold text-gray-500 uppercase tracking-wide">Status</th>
                    <th className="text-right px-6 py-4 text-xs font-semibold text-gray-500 uppercase tracking-wide">Update</th>
                  </tr>
                </thead>
                <tbody>
                  {filtered.map((order, i) => (
                    <tr
                      key={order.orderId}
                      onClick={async () => {
                        try {
                          const res = await api.get('/Orders/all')
                          const fresh = res.data.find(o => o.orderId === order.orderId)
                          setSelectedOrder(fresh || order)
                        } catch {
                          setSelectedOrder(order)
                        }
                      }}
                      className={`border-b border-gray-100 cursor-pointer transition-colors ${selectedOrder?.orderId === order.orderId ? 'bg-orange-50' : 'hover:bg-gray-50'
                        } ${i === filtered.length - 1 ? 'border-0' : ''}`}
                    >
                      <td className="px-6 py-4">
                        <span className="font-medium text-sm" style={{ color: '#1B2A4A' }}>
                          #{order.orderId}
                        </span>
                      </td>
                      <td className="px-6 py-4 text-sm text-gray-500">
                        {new Date(order.dateOrdered).toLocaleDateString()}
                      </td>
                      <td className="px-6 py-4">
                        <span className={`text-xs font-medium px-2.5 py-1 rounded-full border capitalize ${order.deliveryMethod === 'pickup'
                            ? 'bg-orange-50 text-orange-600 border-orange-200'
                            : 'bg-blue-50 text-blue-600 border-blue-200'
                          }`}>
                          {order.deliveryMethod === 'pickup' ? '🏪 Pickup' : '🚚 Shipping'}
                        </span>
                      </td>
                      <td className="px-6 py-4 text-sm font-semibold" style={{ color: '#E8620A' }}>
                        ${order.total.toFixed(2)}
                      </td>
                      <td className="px-6 py-4">
                        <span className={`text-xs font-medium px-2.5 py-1 rounded-full border capitalize ${statusColors[order.orderStatus] || statusColors.pending}`}>
                          {formatStatus(order.orderStatus)}
                        </span>
                      </td>
                      <td className="px-6 py-4">
                        <div className="flex justify-end">
                          <select
                            value={order.orderStatus}
                            onChange={e => {
                              e.stopPropagation()
                              handleStatusUpdate(order.orderId, e.target.value)
                            }}
                            onClick={e => e.stopPropagation()}
                            className="text-xs border border-gray-200 rounded-lg px-2 py-1.5 focus:outline-none focus:ring-2 focus:ring-orange-500"
                          >
                            {getStatuses(order.deliveryMethod).map(s => (
                              <option key={s} value={s}>{formatStatus(s)}</option>
                            ))}
                          </select>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </div>

          {/* Order detail panel */}
          {selectedOrder && (
            <div className="w-80 bg-white rounded-2xl border border-gray-200 p-6 self-start sticky top-6">
              <div className="flex items-center justify-between mb-4">
                <h2 className="font-bold text-lg" style={{ color: '#1B2A4A' }}>Order #{selectedOrder.orderId}</h2>
                <button onClick={() => setSelectedOrder(null)} className="text-gray-400 hover:text-gray-600">✕</button>
              </div>

              <div className="space-y-3 mb-6">
                <div className="flex justify-between text-sm">
                  <span className="text-gray-500">Date</span>
                  <span className="font-medium" style={{ color: '#1B2A4A' }}>
                    {new Date(selectedOrder.dateOrdered).toLocaleDateString()}
                  </span>
                </div>
                <div className="flex justify-between text-sm">
                  <span className="text-gray-500">Delivery</span>
                  <span className={`text-xs font-medium px-2 py-0.5 rounded-full border capitalize ${selectedOrder.deliveryMethod === 'pickup'
                      ? 'bg-orange-50 text-orange-600 border-orange-200'
                      : 'bg-blue-50 text-blue-600 border-blue-200'
                    }`}>
                    {selectedOrder.deliveryMethod === 'pickup' ? '🏪 Pickup' : '🚚 Shipping'}
                  </span>
                </div>
                <div className="flex justify-between text-sm">
                  <span className="text-gray-500">Status</span>
                  <span className={`text-xs font-medium px-2 py-0.5 rounded-full border capitalize ${statusColors[selectedOrder.orderStatus] || statusColors.pending}`}>
                    {formatStatus(selectedOrder.orderStatus)}
                  </span>
                </div>
                <div className="flex justify-between text-sm">
                  <span className="text-gray-500">Total</span>
                  <span className="font-bold" style={{ color: '#E8620A' }}>${selectedOrder.total.toFixed(2)}</span>
                </div>
              </div>

              {(selectedOrder.contactFirstName || selectedOrder.contactEmail) && (
                <div className="border-t border-gray-100 pt-4 mb-6">
                  <h3 className="text-sm font-semibold mb-2" style={{ color: '#1B2A4A' }}>Customer</h3>
                  <div className="text-sm text-gray-600 space-y-0.5">
                    {(selectedOrder.contactFirstName || selectedOrder.contactLastName) && (
                      <p style={{ color: '#1B2A4A' }} className="font-medium">
                        {[selectedOrder.contactFirstName, selectedOrder.contactLastName].filter(Boolean).join(' ')}
                      </p>
                    )}
                    {selectedOrder.contactEmail && <p>{selectedOrder.contactEmail}</p>}
                    {selectedOrder.contactPhone && <p>{selectedOrder.contactPhone}</p>}
                  </div>
                  {selectedOrder.deliveryMethod === 'shipping' && selectedOrder.shippingAddress && (
                    <div className="mt-3 text-sm text-gray-600">
                      <p className="text-xs font-semibold mb-1" style={{ color: '#1B2A4A' }}>Ship to</p>
                      <p>{selectedOrder.shippingAddress}</p>
                      <p>
                        {[selectedOrder.shippingCity, selectedOrder.shippingState, selectedOrder.shippingZip]
                          .filter(Boolean).join(', ')}
                      </p>
                    </div>
                  )}
                </div>
              )}

              <div className="border-t border-gray-100 pt-4 mb-6">
                <h3 className="text-sm font-semibold mb-3" style={{ color: '#1B2A4A' }}>Items</h3>
                <div className="space-y-3">
                  {selectedOrder.items.map((item, i) => {
                    const hasArtwork = item.artworkId || item.ArtworkId
                    const needsArtwork = (item.requiresArtwork ?? item.RequiresArtwork) && !hasArtwork
                    return (
                      <div key={i} className="flex justify-between text-sm">
                        <div>
                          <p className="font-medium" style={{ color: '#1B2A4A' }}>{item.productName}</p>
                          <p className="text-gray-400 text-xs">{item.size} × {item.quantity}</p>
                          {hasArtwork && (
                            <button
                              onClick={() => handleViewArtwork(selectedOrder.orderId, item.variantId)}
                              className="text-xs text-orange-500 hover:text-orange-700 transition-colors mt-0.5 block"
                            >
                              📎 View artwork
                            </button>
                          )}
                          {needsArtwork && (
                            <button
                              onClick={() => handleRegenerateLink(selectedOrder.orderId, item.variantId)}
                              className="text-xs text-orange-500 hover:text-orange-700 transition-colors mt-0.5 block"
                            >
                              ✉️ Resend upload link
                            </button>
                          )}
                        </div>
                        <span className="font-medium" style={{ color: '#1B2A4A' }}>${item.subtotal.toFixed(2)}</span>
                      </div>
                    )
                  })}
                </div>
              </div>

              <div className="border-t border-gray-100 pt-4">
                <h3 className="text-sm font-semibold mb-3" style={{ color: '#1B2A4A' }}>Update status</h3>
                <div className="grid grid-cols-1 gap-2">
                  {getStatuses(selectedOrder.deliveryMethod).map(status => (
                    <button
                      key={status}
                      onClick={() => handleStatusUpdate(selectedOrder.orderId, status)}
                      disabled={selectedOrder.orderStatus === status}
                      className={`py-2 rounded-lg text-xs font-medium capitalize transition-all border ${selectedOrder.orderStatus === status
                          ? `${statusColors[status] || statusColors.pending} cursor-default`
                          : 'border-gray-200 text-gray-600 hover:border-orange-400 hover:text-orange-500'
                        }`}
                    >
                      {selectedOrder.orderStatus === status ? `✓ ` : ''}{formatStatus(status)}
                    </button>
                  ))}
                </div>
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  )
}