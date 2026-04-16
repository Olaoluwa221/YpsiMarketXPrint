import { Link } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'
import { useEffect, useRef } from 'react'

const categories = [
  { name: 'Yard Signs', description: 'Bold, weather-resistant signs for any occasion', emoji: '🪧' },
  { name: 'Banners', description: 'Large format prints for events and promotions', emoji: '🎌' },
  { name: 'Business Cards', description: 'Professional cards that make an impression', emoji: '💼' },
  { name: 'Clothing', description: 'Custom printed apparel for teams and brands', emoji: '👕' },
  { name: 'Mugs', description: 'Personalized mugs for gifts or promotions', emoji: '☕' },
  { name: 'Program Booklets', description: 'Professionally printed event programs', emoji: '📋' },
]

function useFadeUp() {
  const ref = useRef(null)
  useEffect(() => {
    const observer = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (entry.isIntersecting) {
            entry.target.classList.add('opacity-100', 'translate-y-0')
            entry.target.classList.remove('opacity-0', 'translate-y-8')
          }
        })
      },
      { threshold: 0.1 }
    )
    const el = ref.current
    if (el) {
      const children = el.querySelectorAll('.fade-up')
      children.forEach((child, i) => {
        child.style.transitionDelay = `${i * 100}ms`
        observer.observe(child)
      })
    }
    return () => observer.disconnect()
  }, [])
  return ref
}

export default function Home() {
  const { user } = useAuth()
  const categoriesRef = useFadeUp()
  const whyRef = useFadeUp()

  return (
    <div className="min-h-screen bg-gray-50 overflow-x-hidden">

      {/* Hero */}
      <section
        className="relative px-6 py-28 md:py-40 text-center overflow-hidden"
        style={{ background: 'linear-gradient(135deg, #0f1c33 0%, #1B2A4A 50%, #2a3f6b 100%)' }}
      >
        {/* Background decorative circles */}
        <div className="absolute top-0 left-0 w-96 h-96 rounded-full opacity-10"
          style={{ background: '#E8620A', filter: 'blur(80px)', transform: 'translate(-30%, -30%)' }} />
        <div className="absolute bottom-0 right-0 w-96 h-96 rounded-full opacity-10"
          style={{ background: '#E8620A', filter: 'blur(80px)', transform: 'translate(30%, 30%)' }} />

        <div className="relative z-10 max-w-4xl mx-auto">
          <div className="inline-block bg-orange-500/20 text-orange-300 text-sm font-medium px-4 py-2 rounded-full mb-6 border border-orange-500/30"
            style={{ animation: 'fadeUp 0.6s ease forwards' }}>
            Ypsi Marketing & Print Company
          </div>
          <h1
            className="text-5xl md:text-7xl font-bold text-white mb-6 leading-tight"
            style={{ animation: 'fadeUp 0.6s ease 0.1s both' }}
          >
            Print. Market.{' '}
            <span style={{ color: '#E8620A' }}>Deliver.</span>
          </h1>
          <p
            className="text-blue-200 text-lg md:text-xl max-w-2xl mx-auto mb-10"
            style={{ animation: 'fadeUp 0.6s ease 0.2s both' }}
          >
            Professional printing and marketing materials for businesses, events, and individuals — all in one place.
          </p>
          <div
            className="flex flex-col sm:flex-row gap-4 justify-center"
            style={{ animation: 'fadeUp 0.6s ease 0.3s both' }}
          >
            <Link
              to="/products"
              style={{ backgroundColor: '#E8620A' }}
              className="text-white px-8 py-4 rounded-xl font-semibold text-lg hover:opacity-90 hover:scale-105 transition-all duration-200 shadow-lg"
            >
              Shop Now
            </Link>
            {!user && (
              <Link
                to="/register"
                className="text-white px-8 py-4 rounded-xl font-semibold text-lg border-2 border-white/30 hover:border-white hover:bg-white/10 transition-all duration-200"
              >
                Create Account
              </Link>
            )}
          </div>

          {/* Stats row */}
          <div
            className="flex flex-col sm:flex-row gap-8 justify-center mt-16 pt-10 border-t border-white/10"
            style={{ animation: 'fadeUp 0.6s ease 0.4s both' }}
          >
            {[
              { value: '500+', label: 'Happy customers' },
              { value: '10+', label: 'Product types' },
              { value: 'Fast', label: 'Turnaround' },
            ].map((stat) => (
              <div key={stat.label} className="text-center">
                <div className="text-3xl font-bold text-white mb-1">{stat.value}</div>
                <div className="text-blue-300 text-sm">{stat.label}</div>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* Categories */}
      <section className="px-6 py-20 max-w-6xl mx-auto" ref={categoriesRef}>
        <div className="fade-up opacity-0 translate-y-8 transition-all duration-700 text-center mb-12">
          <h2 className="text-4xl font-bold mb-3" style={{ color: '#1B2A4A' }}>What we offer</h2>
          <p className="text-gray-500 text-lg">Everything you need for print and marketing</p>
        </div>

        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6">
          {categories.map((cat) => (
            <Link
              to="/products"
              key={cat.name}
              className="fade-up opacity-0 translate-y-8 transition-all duration-700 bg-white rounded-2xl p-8 border border-gray-200 hover:border-orange-400 hover:shadow-xl group relative overflow-hidden"
            >
              {/* Hover background sweep */}
              <div className="absolute inset-0 opacity-0 group-hover:opacity-100 transition-opacity duration-300 rounded-2xl"
                style={{ background: 'linear-gradient(135deg, #fff8f5 0%, #fff 100%)' }} />
              <div className="relative z-10">
                <div className="text-5xl mb-5 group-hover:scale-110 transition-transform duration-300 inline-block">
                  {cat.emoji}
                </div>
                <h3
                  className="text-xl font-bold mb-2 group-hover:text-orange-500 transition-colors duration-200"
                  style={{ color: '#1B2A4A' }}
                >
                  {cat.name}
                </h3>
                <p className="text-gray-500 text-sm leading-relaxed">{cat.description}</p>
                <div className="mt-4 flex items-center gap-1 text-orange-500 text-sm font-medium opacity-0 group-hover:opacity-100 transition-opacity duration-200">
                  Shop now <span>→</span>
                </div>
              </div>
            </Link>
          ))}
        </div>
      </section>

      {/* Why us */}
      <section className="bg-white px-6 py-20" ref={whyRef}>
        <div className="max-w-6xl mx-auto">
          <div className="fade-up opacity-0 translate-y-8 transition-all duration-700 text-center mb-16">
            <h2 className="text-4xl font-bold mb-3" style={{ color: '#1B2A4A' }}>Why choose us</h2>
            <p className="text-gray-500 text-lg">We make printing simple, fast, and reliable</p>
          </div>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-10">
            {[
              { title: 'Fast turnaround', desc: 'Quick production and delivery so you get your materials when you need them.', emoji: '⚡' },
              { title: 'High quality prints', desc: 'Vibrant colors and sharp detail on every order, guaranteed.', emoji: '✨' },
              { title: 'Easy ordering', desc: 'Upload your design, choose your options, and we handle the rest.', emoji: '🛒' },
            ].map((item) => (
              <div
                key={item.title}
                className="fade-up opacity-0 translate-y-8 transition-all duration-700 text-center px-6 py-8 rounded-2xl hover:bg-gray-50 transition-colors group"
              >
                <div className="text-5xl mb-5 group-hover:scale-110 transition-transform duration-300 inline-block">
                  {item.emoji}
                </div>
                <h3 className="text-xl font-bold mb-3" style={{ color: '#1B2A4A' }}>{item.title}</h3>
                <p className="text-gray-500 leading-relaxed">{item.desc}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* CTA */}
      <section className="px-6 py-20 text-center relative overflow-hidden"
        style={{ background: 'linear-gradient(135deg, #c4510a 0%, #E8620A 50%, #f07830 100%)' }}>
        <div className="absolute top-0 left-0 w-64 h-64 rounded-full opacity-20"
          style={{ background: '#fff', filter: 'blur(60px)', transform: 'translate(-20%, -20%)' }} />
        <div className="relative z-10 max-w-2xl mx-auto">
          <h2 className="text-4xl font-bold text-white mb-4">Ready to get started?</h2>
          <p className="text-orange-100 mb-8 text-lg">Place your first order today and see the difference.</p>
          <Link
            to="/products"
            className="bg-white font-bold px-10 py-4 rounded-xl text-lg hover:scale-105 transition-all duration-200 shadow-lg inline-block"
            style={{ color: '#E8620A' }}
          >
            Browse products
          </Link>
        </div>
      </section>

      {/* Footer */}
      <footer style={{ backgroundColor: '#1B2A4A' }} className="px-6 py-10">
        <div className="max-w-6xl mx-auto flex flex-col md:flex-row items-center justify-between gap-4">
          <span className="text-white font-bold text-lg">Ypsi Marketing & Print</span>
          <div className="flex gap-6">
            <Link to="/products" className="text-blue-300 hover:text-white text-sm transition-colors">Shop</Link>
            <Link to="/register" className="text-blue-300 hover:text-white text-sm transition-colors">Register</Link>
            <Link to="/login" className="text-blue-300 hover:text-white text-sm transition-colors">Login</Link>
          </div>
          <p className="text-blue-400 text-sm">© 2026 Ypsi Marketing & Print Company</p>
        </div>
      </footer>

      <style>{`
        @keyframes fadeUp {
          from { opacity: 0; transform: translateY(2rem); }
          to { opacity: 1; transform: translateY(0); }
        }
      `}</style>

    </div>
  )
}