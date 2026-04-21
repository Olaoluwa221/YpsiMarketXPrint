import { useState, useEffect } from 'react'
import { useParams, Link } from 'react-router-dom'
import api from '../api/axios'

export default function UploadArtwork() {
  const { token } = useParams()
  const [loading, setLoading] = useState(true)
  const [tokenInfo, setTokenInfo] = useState(null)
  const [errorStatus, setErrorStatus] = useState(null) // 'notfound' | 'used' | 'invalidated' | 'orderclosed'
  const [errorMessage, setErrorMessage] = useState('')
  const [uploading, setUploading] = useState(false)
  const [uploaded, setUploaded] = useState(false)
  const [uploadError, setUploadError] = useState('')

  useEffect(() => {
    const fetchTokenInfo = async () => {
      try {
        const res = await api.get(`/artwork-upload/${token}`)
        if (res.data.status === 'valid') {
          setTokenInfo(res.data)
        } else {
          setErrorStatus(res.data.status)
          setErrorMessage(res.data.message)
        }
      } catch (err) {
        if (err.response?.status === 404) {
          setErrorStatus('notfound')
          setErrorMessage(err.response.data?.message || 'This upload link is not valid.')
        } else {
          setErrorStatus('error')
          setErrorMessage('Something went wrong loading this page.')
        }
      } finally {
        setLoading(false)
      }
    }
    fetchTokenInfo()
  }, [token])

  const handleUpload = async (e) => {
    const file = e.target.files[0]
    if (!file) return

    setUploading(true)
    setUploadError('')

    const formData = new FormData()
    formData.append('file', file)

    try {
      await api.post(`/artwork-upload/${token}`, formData, {
        headers: { 'Content-Type': 'multipart/form-data' }
      })
      setUploaded(true)
    } catch (err) {
      setUploadError(err.response?.data || 'Upload failed. Please try again.')
    } finally {
      setUploading(false)
    }
  }

  if (loading) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center px-4">
        <p className="text-gray-400">Loading...</p>
      </div>
    )
  }

  if (errorStatus) {
    const icon = errorStatus === 'used' ? '✓' : '⚠️'
    const iconBg = errorStatus === 'used' ? '#10b981' : '#f59e0b'
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center px-4">
        <div className="bg-white rounded-2xl border border-gray-200 p-8 w-full max-w-md text-center">
          <div
            className="w-16 h-16 rounded-full flex items-center justify-center text-3xl mx-auto mb-4 text-white"
            style={{ backgroundColor: iconBg }}
          >
            {icon}
          </div>
          <h2 className="text-2xl font-bold mb-2" style={{ color: '#1B2A4A' }}>
            {errorStatus === 'used' ? 'Already uploaded' : 'Link unavailable'}
          </h2>
          <p className="text-gray-500 mb-6">{errorMessage}</p>
          <Link to="/" style={{ color: '#E8620A' }} className="text-sm font-medium hover:opacity-80">
            Back to home
          </Link>
        </div>
      </div>
    )
  }

  return (
    <div className="min-h-screen bg-gray-50 flex items-center justify-center px-4 py-8">
      <div className="bg-white rounded-2xl border border-gray-200 p-8 w-full max-w-md">
        <div className="text-center mb-6">
          <h2 className="text-2xl font-bold mb-2" style={{ color: '#1B2A4A' }}>Upload your artwork</h2>
          <p className="text-sm text-gray-500">
            Order <span className="font-semibold" style={{ color: '#1B2A4A' }}>#{tokenInfo.orderId}</span>
          </p>
        </div>

        <div className="bg-gray-50 rounded-xl p-4 mb-6 border border-gray-100">
          <p className="font-semibold" style={{ color: '#1B2A4A' }}>{tokenInfo.productName}</p>
          <p className="text-sm text-gray-500 mt-1">
            Size: {tokenInfo.size} · Quantity: {tokenInfo.quantity}
          </p>
        </div>

        {uploaded ? (
          <div className="text-center py-6">
            <div
              className="w-16 h-16 rounded-full flex items-center justify-center text-3xl mx-auto mb-4 text-white"
              style={{ backgroundColor: '#10b981' }}
            >
              ✓
            </div>
            <p className="font-semibold mb-1" style={{ color: '#1B2A4A' }}>Artwork uploaded!</p>
            <p className="text-sm text-gray-500">We'll get started on your order right away.</p>
          </div>
        ) : (
          <>
            <label
              className={`flex flex-col items-center gap-2 px-4 py-8 rounded-xl border-2 border-dashed cursor-pointer transition-colors ${
                uploading
                  ? 'border-gray-200 text-gray-400'
                  : 'border-orange-300 text-orange-500 hover:border-orange-500 hover:bg-orange-50'
              }`}
            >
              <span className="text-3xl">📎</span>
              <span className="font-medium">
                {uploading ? 'Uploading...' : 'Choose file to upload'}
              </span>
              <input
                type="file"
                accept="image/jpeg,image/png,image/webp,image/gif,application/pdf"
                onChange={handleUpload}
                disabled={uploading}
                className="hidden"
              />
            </label>
            <p className="text-gray-400 text-xs text-center mt-3">
              JPEG, PNG, WebP, GIF or PDF — max 20MB
            </p>
            {uploadError && <p className="text-red-500 text-sm mt-3 text-center">{uploadError}</p>}
          </>
        )}
      </div>
    </div>
  )
}
