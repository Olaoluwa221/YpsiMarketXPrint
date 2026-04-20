import { useState, useEffect } from 'react'
import api from '../../api/axios'
import { useToast } from '../../context/ToastContext'
import { useAuth } from '../../context/AuthContext'

const roleColors = {
    customer: 'bg-gray-100 text-gray-600',
    admin: 'bg-purple-50 text-purple-600',
}

export default function AdminUsers() {
    const { showToast } = useToast()
    const { user: currentUser } = useAuth()
    const [users, setUsers] = useState([])
    const [loading, setLoading] = useState(true)
    const [search, setSearch] = useState('')
    const [filterRole, setFilterRole] = useState('all')
    const [selectedUser, setSelectedUser] = useState(null)
    const [updatingRole, setUpdatingRole] = useState(false)

    useEffect(() => { fetchUsers() }, [])

    const fetchUsers = async () => {
        try {
            const res = await api.get('/Auth/users')
            setUsers(res.data)
        } catch {
            showToast('Failed to load users', 'error')
        } finally {
            setLoading(false)
        }
    }

    const handleRoleUpdate = async (userId, newRole) => {
        setUpdatingRole(true)
        try {
            await api.put(`/Auth/users/${userId}/role`, { role: newRole })
            showToast('Role updated')
            fetchUsers()
            if (selectedUser?.userId === userId) {
                setSelectedUser(prev => ({ ...prev, userType: newRole }))
            }
        } catch {
            showToast('Failed to update role', 'error')
        } finally {
            setUpdatingRole(false)
        }
    }

    const filtered = users.filter(u => {
        const matchesSearch =
            u.email.toLowerCase().includes(search.toLowerCase()) ||
            `${u.firstName} ${u.lastName}`.toLowerCase().includes(search.toLowerCase())
        const matchesRole = filterRole === 'all' || u.userType === filterRole
        return matchesSearch && matchesRole
    })

    return (
        <div className="min-h-screen bg-gray-50 p-6">
            <div className="max-w-7xl mx-auto">

                {/* Header */}
                <div className="mb-8">
                    <h1 className="text-3xl font-bold" style={{ color: '#1B2A4A' }}>Users</h1>
                    <p className="text-gray-500 mt-1">{users.length} registered users</p>
                </div>

                {/* Filters */}
                <div className="flex flex-col sm:flex-row gap-4 mb-6">
                    <input
                        type="text"
                        placeholder="Search by name or email..."
                        value={search}
                        onChange={e => setSearch(e.target.value)}
                        className="flex-1 border border-gray-300 rounded-xl px-4 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-orange-500"
                    />
                    <div className="flex gap-2">
                        {['all', 'customer', 'admin'].map(role => (
                            <button
                                key={role}
                                onClick={() => setFilterRole(role)}
                                className={`px-4 py-2.5 rounded-xl text-sm font-medium capitalize transition-all ${filterRole === role
                                    ? 'text-white shadow-md'
                                    : 'bg-white text-gray-600 border border-gray-200 hover:border-orange-400 hover:text-orange-500'
                                    }`}
                                style={filterRole === role ? { backgroundColor: '#E8620A' } : {}}
                            >
                                {role}
                            </button>
                        ))}
                    </div>
                </div>

                <div className="flex gap-6">
                    {/* Users table */}
                    <div className="flex-1 bg-white rounded-2xl border border-gray-200 overflow-hidden">
                        {loading ? (
                            <div className="p-8 text-center text-gray-400">Loading...</div>
                        ) : filtered.length === 0 ? (
                            <div className="p-16 text-center">
                                <div className="text-5xl mb-4">👥</div>
                                <h3 className="text-lg font-semibold mb-2" style={{ color: '#1B2A4A' }}>No users found</h3>
                                <p className="text-gray-500">Try a different search or filter</p>
                            </div>
                        ) : (
                            <table className="w-full">
                                <thead>
                                    <tr style={{ backgroundColor: '#f8f9fa' }} className="border-b border-gray-200">
                                        <th className="text-left px-6 py-4 text-xs font-semibold text-gray-500 uppercase tracking-wide">User</th>
                                        <th className="text-left px-6 py-4 text-xs font-semibold text-gray-500 uppercase tracking-wide">Role</th>
                                        <th className="text-left px-6 py-4 text-xs font-semibold text-gray-500 uppercase tracking-wide">Orders</th>
                                        <th className="text-left px-6 py-4 text-xs font-semibold text-gray-500 uppercase tracking-wide">Marketing</th>
                                        <th className="text-right px-6 py-4 text-xs font-semibold text-gray-500 uppercase tracking-wide">Actions</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {filtered.map((u, i) => (
                                        <tr
                                            key={u.userId}
                                            onClick={() => setSelectedUser(u)}
                                            className={`border-b border-gray-100 cursor-pointer transition-colors ${selectedUser?.userId === u.userId ? 'bg-orange-50' : 'hover:bg-gray-50'
                                                } ${i === filtered.length - 1 ? 'border-0' : ''}`}
                                        >
                                            <td className="px-6 py-4">
                                                <p className="font-medium text-sm" style={{ color: '#1B2A4A' }}>
                                                    {u.firstName || u.lastName ? `${u.firstName || ''} ${u.lastName || ''}`.trim() : '—'}
                                                </p>
                                                <p className="text-xs text-gray-400 mt-0.5">{u.email}</p>
                                            </td>
                                            <td className="px-6 py-4">
                                                <span className={`text-xs font-medium px-2.5 py-1 rounded-full capitalize ${roleColors[u.userType]}`}>
                                                    {u.userType}
                                                </span>
                                            </td>
                                            <td className="px-6 py-4 text-sm text-gray-600">
                                                {u.orderCount} {u.orderCount === 1 ? 'order' : 'orders'}
                                            </td>
                                            <td className="px-6 py-4">
                                                <span className={`text-xs font-medium px-2.5 py-1 rounded-full ${u.marketingOptIn ? 'bg-green-50 text-green-600' : 'bg-gray-100 text-gray-400'
                                                    }`}>
                                                    {u.marketingOptIn ? 'Opted in' : 'Opted out'}
                                                </span>
                                            </td>
                                            <td className="px-6 py-4">
                                                <div className="flex justify-end">
                                                    <select
                                                        value={u.userType}
                                                        onChange={e => {
                                                            e.stopPropagation()
                                                            handleRoleUpdate(u.userId, e.target.value)
                                                        }}
                                                        onClick={e => e.stopPropagation()}
                                                        disabled={u.userId === currentUser?.userId}
                                                        className="text-xs border border-gray-200 rounded-lg px-2 py-1.5 focus:outline-none focus:ring-2 focus:ring-orange-500 disabled:opacity-50"
                                                    >
                                                        <option value="customer">Customer</option>
                                                        <option value="admin">Admin</option>
                                                    </select>
                                                </div>
                                            </td>
                                        </tr>
                                    ))}
                                </tbody>
                            </table>
                        )}
                    </div>

                    {/* User detail panel */}
                    {selectedUser && (
                        <div className="w-72 bg-white rounded-2xl border border-gray-200 p-6 self-start sticky top-6">
                            <div className="flex items-center justify-between mb-4">
                                <h2 className="font-bold text-lg" style={{ color: '#1B2A4A' }}>User details</h2>
                                <button onClick={() => setSelectedUser(null)} className="text-gray-400 hover:text-gray-600">✕</button>
                            </div>

                            <div className="space-y-3">
                                <div>
                                    <p className="text-xs text-gray-400 mb-0.5">Name</p>
                                    <p className="text-sm font-medium" style={{ color: '#1B2A4A' }}>
                                        {selectedUser.firstName || selectedUser.lastName
                                            ? `${selectedUser.firstName || ''} ${selectedUser.lastName || ''}`.trim()
                                            : '—'}
                                    </p>
                                </div>
                                <div>
                                    <p className="text-xs text-gray-400 mb-0.5">Email</p>
                                    <p className="text-sm font-medium" style={{ color: '#1B2A4A' }}>{selectedUser.email}</p>
                                </div>
                                <div>
                                    <p className="text-xs text-gray-400 mb-0.5">Role</p>
                                    <span className={`text-xs font-medium px-2.5 py-1 rounded-full capitalize ${roleColors[selectedUser.userType]}`}>
                                        {selectedUser.userType}
                                    </span>
                                </div>
                                <div>
                                    <p className="text-xs text-gray-400 mb-0.5">Orders placed</p>
                                    <p className="text-sm font-medium" style={{ color: '#1B2A4A' }}>{selectedUser.orderCount}</p>
                                </div>
                                <div>
                                    <p className="text-xs text-gray-400 mb-0.5">Marketing emails</p>
                                    <p className="text-sm font-medium" style={{ color: '#1B2A4A' }}>
                                        {selectedUser.marketingOptIn ? '✓ Opted in' : '✗ Opted out'}
                                    </p>
                                </div>
                            </div>

                            {selectedUser.userId !== currentUser?.userId && (
                                <div className="border-t border-gray-100 pt-4 mt-4">
                                    <p className="text-xs text-gray-400 mb-2">Change role</p>
                                    <div className="space-y-2">
                                        {['customer', 'admin'].map(role => (
                                            <button
                                                key={role}
                                                onClick={() => handleRoleUpdate(selectedUser.userId, role)}
                                                disabled={selectedUser.userType === role || updatingRole}
                                                className={`w-full py-2 rounded-lg text-xs font-medium capitalize transition-all border ${selectedUser.userType === role
                                                    ? `${roleColors[role]} cursor-default`
                                                    : 'border-gray-200 text-gray-600 hover:border-orange-400 hover:text-orange-500'
                                                    }`}
                                            >
                                                {selectedUser.userType === role ? `✓ ${role}` : role}
                                            </button>
                                        ))}
                                    </div>
                                </div>
                            )}
                        </div>
                    )}
                </div>
            </div>
        </div>
    )
}