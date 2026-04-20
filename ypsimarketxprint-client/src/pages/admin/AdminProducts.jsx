import { useState, useEffect } from 'react'
import api from '../../api/axios'
import { useToast } from '../../context/ToastContext'

export default function AdminProducts() {
  const { showToast } = useToast()
  const [products, setProducts] = useState([])
  const [productTypes, setProductTypes] = useState([])
  const [loading, setLoading] = useState(true)
  const [showProductModal, setShowProductModal] = useState(false)
  const [showTypeModal, setShowTypeModal] = useState(false)
  const [showVariantModal, setShowVariantModal] = useState(false)
  const [showPhotosModal, setShowPhotosModal] = useState(false)
  const [editingProduct, setEditingProduct] = useState(null)
  const [editingVariant, setEditingVariant] = useState(null)
  const [selectedProduct, setSelectedProduct] = useState(null)
  const [newTypeName, setNewTypeName] = useState('')
  const [productForm, setProductForm] = useState({ productName: '', description: '', productTypeId: '', requiresArtwork: false })
  const [variantForm, setVariantForm] = useState({ size: '', price: '' })
  const [uploadingPhoto, setUploadingPhoto] = useState(false)

  useEffect(() => { fetchData() }, [])

  const fetchData = async () => {
    try {
      const [productsRes, typesRes] = await Promise.all([
        api.get('/Products'),
        api.get('/ProductTypes')
      ])
      setProducts(productsRes.data)
      setProductTypes(typesRes.data)
    } catch {
      showToast('Failed to load data', 'error')
    } finally {
      setLoading(false)
    }
  }

  const openCreateProduct = () => {
    setEditingProduct(null)
    setProductForm({ productName: '', description: '', productTypeId: '', requiresArtwork: false })
    setShowProductModal(true)
  }

  const openEditProduct = (product) => {
    setEditingProduct(product)
    setProductForm({
      productName: product.productName,
      description: product.description || '',
      productTypeId: productTypes.find(t => t.typeName === product.productType)?.productTypeId || '',
      requiresArtwork: product.requiresArtwork || false
    })
    setShowProductModal(true)
  }

  const openVariants = (product) => {
    setSelectedProduct(product)
    setEditingVariant(null)
    setVariantForm({ size: '', price: '' })
    setShowVariantModal(true)
  }

  const openPhotos = (product) => {
    setSelectedProduct(product)
    setShowPhotosModal(true)
  }

  const handleProductSubmit = async (e) => {
    e.preventDefault()
    try {
      if (editingProduct) {
        await api.put(`/Products/${editingProduct.productId}`, {
          productName: productForm.productName,
          description: productForm.description,
          productTypeId: parseInt(productForm.productTypeId),
          requiresArtwork: productForm.requiresArtwork
        })
        showToast('Product updated')
      } else {
        await api.post('/Products', {
          productName: productForm.productName,
          description: productForm.description,
          productTypeId: parseInt(productForm.productTypeId),
          requiresArtwork: productForm.requiresArtwork
        })
        showToast('Product created')
      }
      setShowProductModal(false)
      fetchData()
    } catch {
      showToast('Failed to save product', 'error')
    }
  }

  const handleDeleteProduct = async (id) => {
    if (!confirm('Delete this product?')) return
    try {
      await api.delete(`/Products/${id}`)
      showToast('Product deleted')
      fetchData()
    } catch {
      showToast('Failed to delete product', 'error')
    }
  }

  const handleVariantSubmit = async (e) => {
    e.preventDefault()
    try {
      if (editingVariant) {
        await api.put(`/Products/variants/${editingVariant.variantId}`, {
          size: variantForm.size,
          price: parseFloat(variantForm.price)
        })
        showToast('Variant updated')
      } else {
        await api.post(`/Products/${selectedProduct.productId}/variants`, {
          size: variantForm.size,
          price: parseFloat(variantForm.price)
        })
        showToast('Variant added')
      }
      setEditingVariant(null)
      setVariantForm({ size: '', price: '' })
      fetchData()
    } catch {
      showToast('Failed to save variant', 'error')
    }
  }

  const handleDeleteVariant = async (variantId) => {
    if (!confirm('Delete this variant?')) return
    try {
      await api.delete(`/Products/variants/${variantId}`)
      showToast('Variant deleted')
      fetchData()
    } catch {
      showToast('Failed to delete variant', 'error')
    }
  }

  const handlePhotoUpload = async (e) => {
    const file = e.target.files[0]
    if (!file) return
    setUploadingPhoto(true)
    try {
      const formData = new FormData()
      formData.append('file', file)
      const uploadRes = await api.post('/Images/upload', formData, {
        headers: { 'Content-Type': 'multipart/form-data' }
      })
      const isFirst = !selectedProduct.pictures || selectedProduct.pictures.length === 0
      await api.post(`/Images/products/${selectedProduct.productId}/assign`, {
        pictureId: uploadRes.data.pictureId,
        isPrimary: isFirst
      })
      showToast('Photo uploaded')
      fetchData()
    } catch (err) {
      showToast(err.response?.data || 'Failed to upload photo', 'error')
    } finally {
      setUploadingPhoto(false)
      e.target.value = ''
    }
  }

  const handleSetPrimary = async (pictureId) => {
    try {
      await api.put(`/Images/products/${selectedProduct.productId}/primary/${pictureId}`)
      showToast('Primary photo updated')
      fetchData()
    } catch {
      showToast('Failed to set primary photo', 'error')
    }
  }

  const handleDeletePhoto = async (pictureId) => {
    if (!confirm('Remove this photo?')) return
    try {
      await api.delete(`/Images/products/${selectedProduct.productId}/pictures/${pictureId}`)
      showToast('Photo removed')
      fetchData()
    } catch {
      showToast('Failed to remove photo', 'error')
    }
  }

  const handleAddType = async (e) => {
    e.preventDefault()
    try {
      await api.post('/ProductTypes', JSON.stringify(newTypeName), {
        headers: { 'Content-Type': 'application/json' }
      })
      showToast('Product type added')
      setNewTypeName('')
      fetchData()
    } catch (err) {
      showToast(err.response?.data || 'Failed to add type', 'error')
    }
  }

  const handleDeleteType = async (id) => {
    if (!confirm('Delete this product type?')) return
    try {
      await api.delete(`/ProductTypes/${id}`)
      showToast('Product type deleted')
      fetchData()
    } catch (err) {
      showToast(err.response?.data || 'Cannot delete — products exist with this type', 'error')
    }
  }

  useEffect(() => {
    if (selectedProduct) {
      const updated = products.find(p => p.productId === selectedProduct.productId)
      if (updated) setSelectedProduct(updated)
    }
  }, [products])

  return (
    <div className="min-h-screen bg-gray-50 p-6">
      <div className="max-w-7xl mx-auto">

        {/* Header */}
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 mb-8">
          <div>
            <h1 className="text-3xl font-bold" style={{ color: '#1B2A4A' }}>Products</h1>
            <p className="text-gray-500 mt-1">Manage your product catalog</p>
          </div>
          <div className="flex gap-3">
            <button
              onClick={() => setShowTypeModal(true)}
              className="px-4 py-2.5 rounded-xl text-sm font-medium border-2 border-gray-300 hover:border-orange-400 hover:text-orange-500 transition-all"
              style={{ color: '#1B2A4A' }}
            >
              Manage types
            </button>
            <button
              onClick={openCreateProduct}
              style={{ backgroundColor: '#E8620A' }}
              className="px-4 py-2.5 rounded-xl text-sm font-medium text-white hover:opacity-90 transition-opacity"
            >
              + Add product
            </button>
          </div>
        </div>

        {/* Products Table */}
        <div className="bg-white rounded-2xl border border-gray-200 overflow-hidden">
          {loading ? (
            <div className="p-8 text-center text-gray-400">Loading...</div>
          ) : products.length === 0 ? (
            <div className="p-16 text-center">
              <div className="text-5xl mb-4">📦</div>
              <h3 className="text-lg font-semibold mb-2" style={{ color: '#1B2A4A' }}>No products yet</h3>
              <p className="text-gray-500 mb-6">Add your first product to get started</p>
              <button onClick={openCreateProduct} style={{ backgroundColor: '#E8620A' }} className="px-6 py-2.5 rounded-xl text-sm font-medium text-white hover:opacity-90">
                + Add product
              </button>
            </div>
          ) : (
            <table className="w-full">
              <thead>
                <tr style={{ backgroundColor: '#f8f9fa' }} className="border-b border-gray-200">
                  <th className="text-left px-6 py-4 text-xs font-semibold text-gray-500 uppercase tracking-wide">Name</th>
                  <th className="text-left px-6 py-4 text-xs font-semibold text-gray-500 uppercase tracking-wide">Type</th>
                  <th className="text-left px-6 py-4 text-xs font-semibold text-gray-500 uppercase tracking-wide">Variants</th>
                  <th className="text-right px-6 py-4 text-xs font-semibold text-gray-500 uppercase tracking-wide">Actions</th>
                </tr>
              </thead>
              <tbody>
                {products.map((product, i) => (
                  <tr key={product.productId} className={`border-b border-gray-100 hover:bg-gray-50 transition-colors ${i === products.length - 1 ? 'border-0' : ''}`}>
                    <td className="px-6 py-4">
                      <div className="flex items-center gap-3">
                        {product.primaryImageLink ? (
                          <img src={product.primaryImageLink} alt="" className="w-24 h-24 rounded-xl object-cover border border-gray-200 shadow-sm" />
                        ) : (
                          <div className="w-24 h-24 rounded-xl bg-gray-100 flex items-center justify-center text-gray-400 text-3xl border border-gray-200">🖼️</div>
                        )}
                        <div>
                          <span className="font-medium text-sm" style={{ color: '#1B2A4A' }}>{product.productName}</span>
                          {product.description && (
                            <p className="text-xs text-gray-400 mt-0.5">{product.description}</p>
                          )}
                          {product.requiresArtwork && (
                            <span className="text-xs text-purple-500 bg-purple-50 px-2 py-0.5 rounded-full mt-1 inline-block">
                              📎 Requires artwork
                            </span>
                          )}
                        </div>
                      </div>
                    </td>
                    <td className="px-6 py-4">
                      <span className="text-xs font-medium text-orange-500 bg-orange-50 px-2.5 py-1 rounded-full">
                        {product.productType}
                      </span>
                    </td>
                    <td className="px-6 py-4">
                      <div className="flex flex-wrap gap-1">
                        {product.variants?.length > 0 ? (
                          product.variants.map(v => (
                            <span key={v.variantId} className="text-xs bg-gray-100 text-gray-600 px-2 py-1 rounded-md">
                              {v.size} — ${v.price.toFixed(2)}
                            </span>
                          ))
                        ) : (
                          <span className="text-xs text-gray-400">No variants</span>
                        )}
                      </div>
                    </td>
                    <td className="px-6 py-4">
                      <div className="flex items-center justify-end gap-2">
                        <button onClick={() => openVariants(product)} className="text-xs px-3 py-1.5 rounded-lg border border-gray-200 hover:border-orange-400 hover:text-orange-500 transition-all">
                          Variants
                        </button>
                        <button onClick={() => openPhotos(product)} className="text-xs px-3 py-1.5 rounded-lg border border-gray-200 hover:border-orange-400 hover:text-orange-500 transition-all">
                          Photos
                        </button>
                        <button onClick={() => openEditProduct(product)} className="text-xs px-3 py-1.5 rounded-lg border border-gray-200 hover:border-orange-400 hover:text-orange-500 transition-all">
                          Edit
                        </button>
                        <button onClick={() => handleDeleteProduct(product.productId)} className="text-xs px-3 py-1.5 rounded-lg border border-red-200 text-red-500 hover:bg-red-50 transition-all">
                          Delete
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      </div>

      {/* Product Modal */}
      {showProductModal && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
          <div className="bg-white rounded-2xl p-8 w-full max-w-md shadow-2xl">
            <h2 className="text-xl font-bold mb-6" style={{ color: '#1B2A4A' }}>
              {editingProduct ? 'Edit product' : 'Add product'}
            </h2>
            <form onSubmit={handleProductSubmit} className="space-y-4">
              <div>
                <label className="block text-sm font-medium mb-1" style={{ color: '#1B2A4A' }}>Product name</label>
                <input
                  type="text" required value={productForm.productName}
                  onChange={e => setProductForm({ ...productForm, productName: e.target.value })}
                  className="w-full border border-gray-300 rounded-lg px-4 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-orange-500"
                  placeholder="e.g. Classic T-Shirt"
                />
              </div>
              <div>
                <label className="block text-sm font-medium mb-1" style={{ color: '#1B2A4A' }}>
                  Description <span className="text-gray-400 font-normal">(optional)</span>
                </label>
                <textarea
                  value={productForm.description}
                  onChange={e => setProductForm({ ...productForm, description: e.target.value })}
                  className="w-full border border-gray-300 rounded-lg px-4 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-orange-500 resize-none"
                  rows={3} placeholder="Short product description..."
                />
              </div>
              <div>
                <label className="block text-sm font-medium mb-1" style={{ color: '#1B2A4A' }}>Product type</label>
                <select
                  required value={productForm.productTypeId}
                  onChange={e => setProductForm({ ...productForm, productTypeId: e.target.value })}
                  className="w-full border border-gray-300 rounded-lg px-4 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-orange-500"
                >
                  <option value="">Select a type</option>
                  {productTypes.map(type => (
                    <option key={type.productTypeId} value={type.productTypeId}>{type.typeName}</option>
                  ))}
                </select>
              </div>
              <div className="flex items-center gap-3">
                <input
                  type="checkbox"
                  id="requiresArtwork"
                  checked={productForm.requiresArtwork || false}
                  onChange={e => setProductForm({ ...productForm, requiresArtwork: e.target.checked })}
                  className="w-4 h-4 accent-orange-500"
                />
                <label htmlFor="requiresArtwork" className="text-sm text-gray-600">
                  This product requires customer artwork upload
                </label>
              </div>
              <div className="flex gap-3 pt-2">
                <button type="button" onClick={() => setShowProductModal(false)} className="flex-1 py-2.5 rounded-lg border border-gray-300 text-sm font-medium hover:bg-gray-50 transition-colors">
                  Cancel
                </button>
                <button type="submit" style={{ backgroundColor: '#E8620A' }} className="flex-1 py-2.5 rounded-lg text-sm font-medium text-white hover:opacity-90 transition-opacity">
                  {editingProduct ? 'Save changes' : 'Add product'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Variants Modal */}
      {showVariantModal && selectedProduct && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
          <div className="bg-white rounded-2xl p-8 w-full max-w-lg shadow-2xl">
            <div className="flex items-center justify-between mb-6">
              <div>
                <h2 className="text-xl font-bold" style={{ color: '#1B2A4A' }}>Variants</h2>
                <p className="text-sm text-gray-500 mt-0.5">{selectedProduct.productName}</p>
              </div>
              <button onClick={() => setShowVariantModal(false)} className="text-gray-400 hover:text-gray-600 text-xl">✕</button>
            </div>

            <form onSubmit={handleVariantSubmit} className="flex gap-3 mb-6">
              <input
                type="text" required value={variantForm.size}
                onChange={e => setVariantForm({ ...variantForm, size: e.target.value })}
                className="flex-1 border border-gray-300 rounded-lg px-4 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-orange-500"
                placeholder="Size (e.g. Medium, 18x24)"
              />
              <input
                type="number" required step="0.01" min="0" value={variantForm.price}
                onChange={e => setVariantForm({ ...variantForm, price: e.target.value })}
                className="w-28 border border-gray-300 rounded-lg px-4 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-orange-500"
                placeholder="Price"
              />
              <button type="submit" style={{ backgroundColor: '#E8620A' }} className="px-4 py-2.5 rounded-lg text-sm font-medium text-white hover:opacity-90">
                {editingVariant ? 'Save' : 'Add'}
              </button>
              {editingVariant && (
                <button type="button" onClick={() => { setEditingVariant(null); setVariantForm({ size: '', price: '' }) }}
                  className="px-4 py-2.5 rounded-lg text-sm border border-gray-300 hover:bg-gray-50">
                  Cancel
                </button>
              )}
            </form>

            <div className="space-y-2 max-h-64 overflow-y-auto">
              {selectedProduct.variants?.length === 0 ? (
                <p className="text-gray-400 text-sm text-center py-6">No variants yet — add one above</p>
              ) : (
                selectedProduct.variants?.map(variant => (
                  <div key={variant.variantId} className="flex items-center justify-between px-4 py-3 bg-gray-50 rounded-xl">
                    <div>
                      <span className="text-sm font-medium" style={{ color: '#1B2A4A' }}>{variant.size}</span>
                      <span className="text-sm text-gray-500 ml-3">${variant.price.toFixed(2)}</span>
                    </div>
                    <div className="flex gap-2">
                      <button
                        onClick={() => { setEditingVariant(variant); setVariantForm({ size: variant.size, price: variant.price }) }}
                        className="text-xs px-3 py-1.5 rounded-lg border border-gray-200 hover:border-orange-400 hover:text-orange-500 transition-all"
                      >
                        Edit
                      </button>
                      <button
                        onClick={() => handleDeleteVariant(variant.variantId)}
                        className="text-xs px-3 py-1.5 rounded-lg border border-red-200 text-red-500 hover:bg-red-50 transition-all"
                      >
                        Delete
                      </button>
                    </div>
                  </div>
                ))
              )}
            </div>
          </div>
        </div>
      )}

      {/* Photos Modal */}
      {showPhotosModal && selectedProduct && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
          <div className="bg-white rounded-2xl p-8 w-full max-w-lg shadow-2xl">
            <div className="flex items-center justify-between mb-6">
              <div>
                <h2 className="text-xl font-bold" style={{ color: '#1B2A4A' }}>Photos</h2>
                <p className="text-sm text-gray-500 mt-0.5">{selectedProduct.productName}</p>
              </div>
              <button onClick={() => setShowPhotosModal(false)} className="text-gray-400 hover:text-gray-600 text-xl">✕</button>
            </div>

            <label className={`flex items-center justify-center gap-2 w-full py-4 rounded-xl border-2 border-dashed border-gray-300 hover:border-orange-400 cursor-pointer transition-colors mb-6 ${uploadingPhoto ? 'opacity-50 pointer-events-none' : ''}`}>
              <span className="text-sm text-gray-500">{uploadingPhoto ? 'Uploading...' : '+ Upload photo'}</span>
              <input type="file" accept="image/jpeg,image/png,image/webp" className="hidden" onChange={handlePhotoUpload} disabled={uploadingPhoto} />
            </label>

            <div className="grid grid-cols-3 gap-3 max-h-72 overflow-y-auto">
              {!selectedProduct.pictures || selectedProduct.pictures.length === 0 ? (
                <div className="col-span-3 text-center py-8 text-gray-400 text-sm">No photos yet</div>
              ) : (
                selectedProduct.pictures.map(pic => (
                  <div key={pic.pictureId} className="relative group rounded-xl overflow-hidden border border-gray-200">
                    <img src={pic.link} alt="" className="w-full h-24 object-cover" />
                    {pic.isPrimary && (
                      <div className="absolute top-1 left-1 bg-orange-500 text-white text-xs px-1.5 py-0.5 rounded-md">
                        Primary
                      </div>
                    )}
                    <div className="absolute inset-0 bg-black/50 opacity-0 group-hover:opacity-100 transition-opacity flex flex-col items-center justify-center gap-1">
                      {!pic.isPrimary && (
                        <button
                          onClick={() => handleSetPrimary(pic.pictureId)}
                          className="text-xs text-white bg-orange-500 px-2 py-1 rounded-lg hover:opacity-90"
                        >
                          Set primary
                        </button>
                      )}
                      <button
                        onClick={() => handleDeletePhoto(pic.pictureId)}
                        className="text-xs text-white bg-red-500 px-2 py-1 rounded-lg hover:opacity-90"
                      >
                        Remove
                      </button>
                    </div>
                  </div>
                ))
              )}
            </div>
          </div>
        </div>
      )}

      {/* Product Types Modal */}
      {showTypeModal && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
          <div className="bg-white rounded-2xl p-8 w-full max-w-md shadow-2xl">
            <h2 className="text-xl font-bold mb-6" style={{ color: '#1B2A4A' }}>Manage product types</h2>
            <form onSubmit={handleAddType} className="flex gap-2 mb-6">
              <input
                type="text" required value={newTypeName}
                onChange={e => setNewTypeName(e.target.value)}
                className="flex-1 border border-gray-300 rounded-lg px-4 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-orange-500"
                placeholder="New type name"
              />
              <button type="submit" style={{ backgroundColor: '#E8620A' }} className="px-4 py-2.5 rounded-lg text-sm font-medium text-white hover:opacity-90">
                Add
              </button>
            </form>
            <div className="space-y-2 max-h-64 overflow-y-auto">
              {productTypes.length === 0 ? (
                <p className="text-gray-400 text-sm text-center py-4">No product types yet</p>
              ) : (
                productTypes.map(type => (
                  <div key={type.productTypeId} className="flex items-center justify-between py-2.5 px-4 bg-gray-50 rounded-lg">
                    <span className="text-sm font-medium" style={{ color: '#1B2A4A' }}>{type.typeName}</span>
                    <button onClick={() => handleDeleteType(type.productTypeId)} className="text-xs text-red-500 hover:text-red-700 transition-colors">
                      Delete
                    </button>
                  </div>
                ))
              )}
            </div>
            <button onClick={() => setShowTypeModal(false)} className="w-full mt-6 py-2.5 rounded-lg border border-gray-300 text-sm font-medium hover:bg-gray-50 transition-colors">
              Done
            </button>
          </div>
        </div>
      )}
    </div>
  )
}