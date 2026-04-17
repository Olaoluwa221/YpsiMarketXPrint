import { useState, useEffect } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import api from '../api/axios'
import { useAuth } from '../context/AuthContext'
import { useToast } from '../context/ToastContext'

export default function ProductDetail() {
  const { id } = useParams()
  const { user } = useAuth()
  const { showToast } = useToast()
  const navigate = useNavigate()
  const [product, setProduct] = useState(null)
  const [loading, setLoading] = useState(true)
  const [selectedVariant, setSelectedVariant] = useState(null)
  const [quantity, setQuantity] = useState(1)
  const [addingToCart, setAddingToCart] = useState(false)
  const [selectedImage, setSelectedImage] = useState(null)

  useEffect(() => {
    const fetchProduct = async () => {
      try {
        const res = await api.get(`/Products/${id}`)
        setProduct(res.data)
        if (res.data.variants?.length > 0) {
          setSelectedVariant(res.data.variants[0])
        }
        setSelectedImage(res.data.primaryImageLink || res.data.pictures?.[0]?.link || null)
      } catch {
        navigate('/products')
      } finally {
        setLoading(false)
      }
    }
    fetchProduct()
  }, [id])

  const handleAddToCart = async () => {
    if (!selectedVariant) {
      showToast('Please select a size', 'error')
      return
    }
    if (!user) {
      navigate('/login')
      return
    }
    setAddingToCart(true)
    try {
      await api.post('/Cart/items', {
        variantId: selectedVariant.variantId,
        quantity
      })
      showToast('Added to cart!')
    } catch {
      showToast('Failed to add to cart', 'error')
    } finally {
      setAddingToCart(false)
    }
  }

  if (loading) return (
    <div className="min-h-screen bg-gray-50 flex items-center justify-center">
      <div className="animate-pulse text-gray-400 text-lg">Loading...</div>
    </div>
  )

  if (!product) return null

  const pictures = product.pictures || []

  return (
    <div className="min-h-screen bg-gray-50">
      <div className="max-w-6xl mx-auto px-6 py-12">

        {/* Back */}
        <button
          onClick={() => navigate('/products')}
          className="flex items-center gap-2 text-sm text-gray-500 hover:text-orange-500 transition-colors mb-8"
        >
          ← Back to products
        </button>

        <div className="grid grid-cols-1 lg:grid-cols-2 gap-12">

          {/* Images */}
          <div>
            <div className="bg-white rounded-2xl border border-gray-200 overflow-hidden mb-4 aspect-square flex items-center justify-center">
              {selectedImage ? (
                <img src={selectedImage} alt={product.productName} className="w-full h-full object-cover" />
              ) : (
                <span className="text-8xl">🖨️</span>
              )}
            </div>

            {/* Thumbnail strip */}
            {pictures.length > 1 && (
              <div className="flex gap-3 overflow-x-auto pb-2">
                {pictures.map(pic => (
                  <button
                    key={pic.pictureId}
                    onClick={() => setSelectedImage(pic.link)}
                    className={`shrink-0 w-16 h-16 rounded-xl overflow-hidden border-2 transition-all ${
                      selectedImage === pic.link ? 'border-orange-500' : 'border-gray-200 hover:border-orange-300'
                    }`}
                  >
                    <img src={pic.link} alt="" className="w-full h-full object-cover" />
                  </button>
                ))}
              </div>
            )}
          </div>

          {/* Info */}
          <div>
            <span className="text-xs font-medium text-orange-500 bg-orange-50 px-3 py-1 rounded-full">
              {product.productType}
            </span>

            <h1 className="text-3xl font-bold mt-3 mb-3" style={{ color: '#1B2A4A' }}>
              {product.productName}
            </h1>

            {product.description && (
              <p className="text-gray-500 mb-6 leading-relaxed">{product.description}</p>
            )}

            {/* Price */}
            {selectedVariant && (
              <div className="text-4xl font-bold mb-6" style={{ color: '#E8620A' }}>
                ${selectedVariant.price.toFixed(2)}
              </div>
            )}

            {/* Variant selector */}
            {product.variants?.length > 0 ? (
              <div className="mb-6">
                <label className="block text-sm font-semibold mb-3" style={{ color: '#1B2A4A' }}>
                  Size / Option
                </label>
                <div className="flex flex-wrap gap-3">
                  {product.variants.map(variant => (
                    <button
                      key={variant.variantId}
                      onClick={() => setSelectedVariant(variant)}
                      className={`px-5 py-2.5 rounded-xl text-sm font-medium border-2 transition-all ${
                        selectedVariant?.variantId === variant.variantId
                          ? 'text-white border-orange-500'
                          : 'border-gray-200 text-gray-600 hover:border-orange-400'
                      }`}
                      style={selectedVariant?.variantId === variant.variantId ? { backgroundColor: '#E8620A' } : {}}
                    >
                      {variant.size}
                      <span className="ml-2 opacity-75">${variant.price.toFixed(2)}</span>
                    </button>
                  ))}
                </div>
              </div>
            ) : (
              <div className="mb-6 p-4 bg-yellow-50 border border-yellow-200 rounded-xl text-sm text-yellow-700">
                No variants available yet — check back soon.
              </div>
            )}

            {/* Quantity */}
            {product.variants?.length > 0 && (
              <div className="mb-8">
                <label className="block text-sm font-semibold mb-3" style={{ color: '#1B2A4A' }}>
                  Quantity
                </label>
                <div className="flex items-center gap-3">
                  <button
                    onClick={() => setQuantity(q => Math.max(1, q - 1))}
                    className="w-10 h-10 rounded-xl border-2 border-gray-200 flex items-center justify-center text-gray-600 hover:border-orange-400 hover:text-orange-500 transition-all font-bold text-lg"
                  >
                    −
                  </button>
                  <span className="w-12 text-center font-semibold text-lg" style={{ color: '#1B2A4A' }}>
                    {quantity}
                  </span>
                  <button
                    onClick={() => setQuantity(q => q + 1)}
                    className="w-10 h-10 rounded-xl border-2 border-gray-200 flex items-center justify-center text-gray-600 hover:border-orange-400 hover:text-orange-500 transition-all font-bold text-lg"
                  >
                    +
                  </button>
                </div>
              </div>
            )}

            {/* Add to cart */}
            <button
              onClick={handleAddToCart}
              disabled={addingToCart || !selectedVariant}
              style={{ backgroundColor: '#E8620A' }}
              className="w-full py-4 rounded-xl text-white font-semibold text-lg hover:opacity-90 disabled:opacity-50 transition-opacity shadow-lg"
            >
              {addingToCart ? 'Adding...' : !user ? 'Login to add to cart' : 'Add to cart'}
            </button>

            {!user && (
              <p className="text-center text-sm text-gray-400 mt-3">
                You need to be logged in to add items to your cart
              </p>
            )}
          </div>
        </div>
      </div>
    </div>
  )
}