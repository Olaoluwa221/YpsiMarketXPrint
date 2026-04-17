import { useState, useEffect } from 'react'
import { Link } from 'react-router-dom'
import api from '../api/axios'
import { SkeletonCard } from '../components/Skeleton'

export default function Products() {
  const [products, setProducts] = useState([])
  const [productTypes, setProductTypes] = useState([])
  const [loading, setLoading] = useState(true)
  const [search, setSearch] = useState('')
  const [activeCategory, setActiveCategory] = useState('All')

  useEffect(() => {
    const fetchData = async () => {
      try {
        const [productsRes, typesRes] = await Promise.all([
          api.get('/Products'),
          api.get('/ProductTypes')
        ])
        setProducts(productsRes.data)
        setProductTypes(typesRes.data)
      } catch (err) {
        console.error('Failed to fetch products', err)
      } finally {
        setLoading(false)
      }
    }
    fetchData()
  }, [])

  const filtered = products.filter(p => {
    const matchesSearch = p.productName.toLowerCase().includes(search.toLowerCase())
    const matchesCategory = activeCategory === 'All' || p.productType === activeCategory
    return matchesSearch && matchesCategory
  })

  const getStartingPrice = (variants) => {
    if (!variants || variants.length === 0) return null
    return Math.min(...variants.map(v => v.price))
  }

  return (
    <div className="min-h-screen bg-gray-50">

      {/* Header */}
      <section style={{ backgroundColor: '#1B2A4A' }} className="px-6 py-14 text-center">
        <h1 className="text-4xl font-bold text-white mb-3">Our Products</h1>
        <p className="text-blue-200 text-lg mb-8">Browse our full catalog of print and marketing materials</p>

        {/* Search */}
        <div className="max-w-xl mx-auto relative">
          <span className="absolute left-4 top-1/2 -translate-y-1/2 text-gray-400 text-lg">🔍</span>
          <input
            type="text"
            placeholder="Search products..."
            value={search}
            onChange={e => setSearch(e.target.value)}
            className="w-full pl-12 pr-4 py-4 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-orange-500 bg-white shadow-lg"
          />
        </div>
      </section>

      <div className="max-w-7xl mx-auto px-6 py-10">

        {/* Category filters */}
        <div className="flex gap-3 flex-wrap mb-10">
          {['All', ...productTypes.map(t => t.typeName)].map(cat => (
            <button
              key={cat}
              onClick={() => setActiveCategory(cat)}
              className={`px-5 py-2.5 rounded-full text-sm font-medium transition-all duration-200 ${activeCategory === cat
                  ? 'text-white shadow-lg scale-105'
                  : 'bg-white text-gray-600 border border-gray-200 hover:border-orange-400 hover:text-orange-500'
                }`}
              style={activeCategory === cat ? { backgroundColor: '#E8620A' } : {}}
            >
              {cat}
            </button>
          ))}
        </div>

        {/* Results count */}
        {!loading && (
          <p className="text-gray-500 text-sm mb-6">
            {filtered.length} {filtered.length === 1 ? 'product' : 'products'} found
          </p>
        )}

        {/* Grid */}
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6">
          {loading
            ? Array.from({ length: 8 }).map((_, i) => <SkeletonCard key={i} />)
            : filtered.length === 0
              ? (
                <div className="col-span-full text-center py-20">
                  <div className="text-5xl mb-4">🔍</div>
                  <h3 className="text-xl font-semibold mb-2" style={{ color: '#1B2A4A' }}>No products found</h3>
                  <p className="text-gray-500">Try a different search or category</p>
                </div>
              )
              : filtered.map(product => {
                const startingPrice = getStartingPrice(product.variants)
                return (
                  <Link
                    to={`/products/${product.productId}`}
                    key={product.productId}
                    className="bg-white rounded-2xl border border-gray-200 hover:border-orange-400 hover:shadow-xl transition-all duration-300 group overflow-hidden"
                  >
                    {/* Image */}
                    <div
                      className="h-48 flex items-center justify-center overflow-hidden"
                      style={{ backgroundColor: '#f8f4f1' }}
                    >
                      {product.primaryImageLink ? (
                        <img
                          src={product.primaryImageLink}
                          alt={product.productName}
                          className="h-full w-full object-cover group-hover:scale-105 transition-transform duration-300"
                        />
                      ) : (
                        <span className="text-5xl group-hover:scale-110 transition-transform duration-300">🖨️</span>
                      )}
                    </div>

                    {/* Info */}
                    <div className="p-5">
                      <span className="text-xs font-medium text-orange-500 bg-orange-50 px-2 py-1 rounded-full">
                        {product.productType}
                      </span>
                      <h3 className="text-base font-semibold mt-2 mb-1 group-hover:text-orange-500 transition-colors" style={{ color: '#1B2A4A' }}>
                        {product.productName}
                      </h3>
                      {product.description && (
                        <p className="text-gray-400 text-xs mb-3 line-clamp-2">{product.description}</p>
                      )}
                      <div className="flex items-center justify-between mt-3">
                        {startingPrice !== null ? (
                          <span className="text-lg font-bold" style={{ color: '#E8620A' }}>
                            From ${startingPrice.toFixed(2)}
                          </span>
                        ) : (
                          <span className="text-sm text-gray-400">No variants yet</span>
                        )}
                        <span className="text-xs text-gray-400 group-hover:text-orange-500 transition-colors font-medium">
                          View →
                        </span>
                      </div>
                    </div>
                  </Link>
                )
              })
          }
        </div>
      </div>
    </div>
  )
}