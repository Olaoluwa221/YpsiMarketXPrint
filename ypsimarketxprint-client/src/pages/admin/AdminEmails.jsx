import { useState, useEffect } from 'react'
import api from '../../api/axios'
import { useToast } from '../../context/ToastContext'

export default function AdminEmails() {
  const { showToast } = useToast()
  const [optedInCount, setOptedInCount] = useState(null)
  const [sending, setSending] = useState(false)
  const [form, setForm] = useState({
    subject: '',
    body: ''
  })
  const [preview, setPreview] = useState(false)

  useEffect(() => {
    const fetchOptedIn = async () => {
      try {
        const res = await api.get('/Auth/opted-in-count')
        setOptedInCount(res.data.count)
      } catch {
        console.error('Failed to fetch opted-in count')
      }
    }
    fetchOptedIn()
  }, [])

  const handleSend = async () => {
    if (!form.subject.trim()) {
      showToast('Subject is required', 'error')
      return
    }
    if (!form.body.trim()) {
      showToast('Email body is required', 'error')
      return
    }
    if (!confirm(`Send this email to ${optedInCount} opted-in customers?`)) return

    setSending(true)
    try {
      await api.post('/Auth/send-promotional', {
        subject: form.subject,
        htmlBody: form.body
      })
      showToast('Emails sent successfully!')
      setForm({ subject: '', body: '' })
    } catch {
      showToast('Failed to send emails', 'error')
    } finally {
      setSending(false)
    }
  }

  return (
    <div className="min-h-screen bg-gray-50 p-6">
      <div className="max-w-4xl mx-auto">

        {/* Header */}
        <div className="mb-8">
          <h1 className="text-3xl font-bold" style={{ color: '#1B2A4A' }}>Email campaigns</h1>
          <p className="text-gray-500 mt-1">
            Send promotional emails to opted-in customers
            {optedInCount !== null && (
              <span className="ml-2 text-sm font-medium px-2 py-0.5 rounded-full bg-orange-50 text-orange-500 border border-orange-200">
                {optedInCount} subscribers
              </span>
            )}
          </p>
        </div>

        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">

          {/* Compose */}
          <div className="bg-white rounded-2xl border border-gray-200 p-6">
            <h2 className="text-lg font-bold mb-6" style={{ color: '#1B2A4A' }}>Compose</h2>

            <div className="space-y-4">
              <div>
                <label className="block text-sm font-medium mb-1" style={{ color: '#1B2A4A' }}>Subject</label>
                <input
                  type="text"
                  value={form.subject}
                  onChange={e => setForm({ ...form, subject: e.target.value })}
                  placeholder="e.g. Summer sale — 20% off all prints!"
                  className="w-full border border-gray-300 rounded-lg px-4 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-orange-500"
                />
              </div>

              <div>
                <label className="block text-sm font-medium mb-1" style={{ color: '#1B2A4A' }}>Body</label>
                <p className="text-xs text-gray-400 mb-2">You can use basic HTML for formatting</p>
                <textarea
                  value={form.body}
                  onChange={e => setForm({ ...form, body: e.target.value })}
                  placeholder="<h2>Big news!</h2><p>We're running a special promotion...</p>"
                  rows={12}
                  className="w-full border border-gray-300 rounded-lg px-4 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-orange-500 font-mono resize-none"
                />
              </div>

              <div className="flex gap-3">
                <button
                  onClick={() => setPreview(!preview)}
                  className="flex-1 py-2.5 rounded-xl text-sm font-medium border-2 transition-colors"
                  style={{ color: '#1B2A4A', borderColor: '#1B2A4A' }}
                >
                  {preview ? 'Hide preview' : 'Preview'}
                </button>
                <button
                  onClick={handleSend}
                  disabled={sending || optedInCount === 0}
                  style={{ backgroundColor: '#E8620A' }}
                  className="flex-1 py-2.5 rounded-xl text-sm font-semibold text-white hover:opacity-90 disabled:opacity-50 transition-opacity"
                >
                  {sending ? 'Sending...' : `Send to ${optedInCount ?? '...'} subscribers`}
                </button>
              </div>

              {optedInCount === 0 && (
                <p className="text-xs text-center text-gray-400">No customers have opted in to marketing emails yet.</p>
              )}
            </div>
          </div>

          {/* Preview */}
          <div className="bg-white rounded-2xl border border-gray-200 p-6">
            <h2 className="text-lg font-bold mb-6" style={{ color: '#1B2A4A' }}>
              {preview ? 'Preview' : 'Tips'}
            </h2>

            {preview ? (
              <div className="border border-gray-200 rounded-xl overflow-hidden">
                {/* Email header */}
                <div style={{ backgroundColor: '#1B2A4A' }} className="px-6 py-4 text-center">
                  <p className="text-white font-bold text-lg">Ypsi Marketing & Print</p>
                </div>
                {/* Email body */}
                <div className="p-6 bg-gray-50">
                  {form.body ? (
                    <div
                      className="text-sm text-gray-700"
                      dangerouslySetInnerHTML={{ __html: form.body }}
                    />
                  ) : (
                    <p className="text-gray-400 text-sm italic">Your email body will appear here...</p>
                  )}
                </div>
                {/* Email footer */}
                <div style={{ backgroundColor: '#1B2A4A' }} className="px-6 py-3 text-center">
                  <p className="text-xs" style={{ color: '#8899bb' }}>
                    © 2026 Ypsi Marketing & Print Company. You're receiving this because you opted in to marketing emails.
                  </p>
                </div>
              </div>
            ) : (
              <div className="space-y-4">
                <div className="flex gap-3 p-4 bg-blue-50 rounded-xl border border-blue-100">
                  <span className="text-xl">✍️</span>
                  <div>
                    <p className="text-sm font-medium text-blue-800">Use HTML for formatting</p>
                    <p className="text-xs text-blue-600 mt-0.5">Use tags like &lt;h2&gt;, &lt;p&gt;, &lt;strong&gt;, &lt;a href=""&gt; to style your email</p>
                  </div>
                </div>
                <div className="flex gap-3 p-4 bg-green-50 rounded-xl border border-green-100">
                  <span className="text-xl">👀</span>
                  <div>
                    <p className="text-sm font-medium text-green-800">Preview before sending</p>
                    <p className="text-xs text-green-600 mt-0.5">Click Preview to see how your email will look to customers</p>
                  </div>
                </div>
                <div className="flex gap-3 p-4 bg-orange-50 rounded-xl border border-orange-200">
                  <span className="text-xl">⚠️</span>
                  <div>
                    <p className="text-sm font-medium text-orange-800">Emails cannot be unsent</p>
                    <p className="text-xs text-orange-600 mt-0.5">Double check your subject and body before hitting send</p>
                  </div>
                </div>
                <div className="flex gap-3 p-4 bg-purple-50 rounded-xl border border-purple-100">
                  <span className="text-xl">📋</span>
                  <div>
                    <p className="text-sm font-medium text-purple-800">Example body</p>
                    <p className="text-xs text-purple-600 mt-0.5 font-mono">&lt;h2&gt;Big news!&lt;/h2&gt;&lt;p&gt;We're running a special promotion this week...&lt;/p&gt;</p>
                  </div>
                </div>
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  )
}