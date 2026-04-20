import { useState } from 'react'
import api from '../api/axios'

export default function ArtworkUploader({ orderId, variantId, productName }) {
  const [uploading, setUploading] = useState(false)
  const [uploaded, setUploaded] = useState(false)
  const [error, setError] = useState('')

  const handleUpload = async (e) => {
    const file = e.target.files[0]
    if (!file) return

    setUploading(true)
    setError('')

    const formData = new FormData()
    formData.append('file', file)

    try {
      await api.post(`/Images/orders/${orderId}/artwork/${variantId}`, formData, {
        headers: { 'Content-Type': 'multipart/form-data' }
      })
      setUploaded(true)
    } catch {
      setError('Upload failed. Please try again.')
    } finally {
      setUploading(false)
    }
  }

  return (
    <div className="mb-3 last:mb-0">
      <p className="text-xs font-medium mb-1.5" style={{ color: '#1B2A4A' }}>{productName}</p>
      {uploaded ? (
        <div className="flex items-center gap-2 text-green-600 text-xs font-medium">
          <span>✓</span>
          <span>Artwork uploaded successfully</span>
        </div>
      ) : (
        <div>
          <label className={`flex items-center gap-2 px-3 py-2 rounded-lg border-2 border-dashed cursor-pointer transition-colors text-xs ${
            uploading ? 'border-gray-200 text-gray-400' : 'border-orange-300 text-orange-500 hover:border-orange-500 hover:bg-orange-50'
          }`}>
            <span>{uploading ? 'Uploading...' : '📎 Choose file'}</span>
            <input
              type="file"
              accept="image/jpeg,image/png,image/webp,image/gif,application/pdf"
              onChange={handleUpload}
              disabled={uploading}
              className="hidden"
            />
          </label>
          {error && <p className="text-red-500 text-xs mt-1">{error}</p>}
          <p className="text-gray-400 text-xs mt-1">JPEG, PNG, WebP, GIF or PDF — max 20MB</p>
        </div>
      )}
    </div>
  )
}