import { useEffect, useState } from 'react'

export default function BackToTop() {
  const [visible, setVisible] = useState(false)

  useEffect(() => {
    const handleScroll = () => setVisible(window.scrollY > 400)
    window.addEventListener('scroll', handleScroll)
    return () => window.removeEventListener('scroll', handleScroll)
  }, [])

  const scrollToTop = () => window.scrollTo({ top: 0, behavior: 'smooth' })

  if (!visible) return null

  return (
    <button
      onClick={scrollToTop}
      style={{ backgroundColor: '#E8620A' }}
      className="fixed bottom-6 left-6 z-50 w-12 h-12 rounded-full text-white shadow-lg hover:opacity-90 hover:scale-110 transition-all duration-200 flex items-center justify-center text-xl"
    >
      ↑
    </button>
  )
}