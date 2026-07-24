import { useState } from 'react'
import { Link, useNavigate, Outlet } from 'react-router-dom'
import { useAuth } from '../components/AuthContext'
import { Button } from '../components/ui/button'
import { 
  User, 
  Menu, 
  X, 
  ChevronDown, 
  LogOut, 
  Users, 
  Lock, 
  BookOpen,
  FileText
} from 'lucide-react'

export default function AdminLayout() {
  const { user, logout, hasPermission } = useAuth()
  const navigate = useNavigate()
  
  // Mobile drawer open state
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false)
  // Profile dropdown open state
  const [profileDropdownOpen, setProfileDropdownOpen] = useState(false)

  const handleLogout = () => {
    logout()
    navigate('/login')
  }

  // Dynamic menu item configuration based on claims
  const menuItems = [
    {
      label: 'Home',
      path: user?.roles.includes('Admin') ? '/admin' : '/',
      permission: null
    },
    {
      label: 'Users Management',
      path: '/admin/users',
      permission: 'Permission.Identity.Users.Read',
      icon: <Users className="w-4 h-4 mr-2" />
    },
    {
      label: 'Roles Management',
      path: '/admin/roles',
      permission: 'Permission.Identity.Roles.Read',
      icon: <Lock className="w-4 h-4 mr-2" />
    },
    {
      label: 'Categories Management',
      path: '/admin/categories',
      permission: 'Permission.Product.Categories.Read',
      icon: <BookOpen className="w-4 h-4 mr-2" />
    },
    {
      label: 'Audit Logs',
      path: '/admin/audit-logs',
      permission: 'Permission.Identity.AuditTrails.Read',
      icon: <FileText className="w-4 h-4 mr-2" />
    },
    {
      label: 'My Profile',
      path: '/profile',
      permission: null,
      icon: <User className="w-4 h-4 mr-2" />
    }
  ]

  // Filter items matching user's permissions
  const activeMenuItems = menuItems.filter(item => {
    if (!item.permission) return true
    return hasPermission(item.permission)
  })

  return (
    <div className="min-h-screen bg-neutral-100 flex flex-col font-sans">
      
      {/* --- ADMIN HEADER AREA --- */}
      <header className="sticky top-0 z-40 w-full border-b border-neutral-200 bg-white/95 backdrop-blur-md supports-[backdrop-filter]:bg-white/60">
        <div className="container mx-auto px-4 md:px-6 h-16 flex items-center justify-between">
          
          {/* Logo Section */}
          <div className="flex items-center gap-6">
            <Link to="/" className="flex items-center space-x-2">
              <BookOpen className="w-6 h-6 text-[#4285F4]" />
              <span className="font-bold text-xl tracking-tight text-neutral-900">Bookjar</span>
            </Link>

            {/* Desktop Navigation Link Menu */}
            <nav className="hidden md:flex items-center gap-6 text-sm font-semibold text-neutral-600">
              {activeMenuItems.map((item) => (
                <Link
                  key={item.label}
                  to={item.path}
                  className="transition-colors hover:text-[#4285F4] flex items-center"
                >
                  {item.label}
                </Link>
              ))}
            </nav>
          </div>

          {/* Right Area: Profile dropdown & Mobile Toggle */}
          <div className="flex items-center gap-3">
            
            {/* User Session Info / Profile Dropdown Menu */}
            {user ? (
              <div className="relative">
                <Button
                  variant="ghost"
                  onClick={() => setProfileDropdownOpen(!profileDropdownOpen)}
                  className="flex items-center gap-2 px-3 py-1.5 h-10 hover:bg-neutral-100 rounded-xl"
                >
                  <div className="w-7 h-7 bg-[#4285F4]/10 rounded-full flex items-center justify-center text-[#4285F4] font-bold text-xs uppercase border border-[#4285F4]/20">
                    {user.fullName ? user.fullName[0] : 'U'}
                  </div>
                  <span className="hidden sm:inline text-sm font-bold text-neutral-700">{user.fullName}</span>
                  <ChevronDown className={`w-4 h-4 text-neutral-400 transition-transform ${profileDropdownOpen ? 'rotate-180' : ''}`} />
                </Button>

                {/* Dropdown Menu Container */}
                {profileDropdownOpen && (
                  <>
                    <div 
                      className="fixed inset-0 z-30" 
                      onClick={() => setProfileDropdownOpen(false)}
                    />
                    <div className="absolute right-0 mt-2 w-56 rounded-xl border border-neutral-200 bg-white p-1 text-neutral-950 shadow-lg ring-1 ring-black/5 z-40 animate-in fade-in slide-in-from-top-2 duration-150">
                      <div className="px-3 py-2 border-b border-neutral-100 mb-1">
                        <p className="text-xs font-semibold text-neutral-400 uppercase tracking-wider">Account</p>
                        <p className="text-sm font-bold truncate text-neutral-800">{user.email}</p>
                      </div>
                      <Link
                        to="/profile"
                        onClick={() => setProfileDropdownOpen(false)}
                        className="flex w-full items-center px-3 py-2 text-sm text-neutral-700 hover:bg-neutral-50 rounded-lg font-medium transition-colors"
                      >
                        <User className="w-4 h-4 mr-2 text-neutral-400" />
                        My Profile
                      </Link>
                      <button
                        onClick={() => {
                          setProfileDropdownOpen(false)
                          handleLogout()
                        }}
                        className="flex w-full items-center px-3 py-2 text-sm text-rose-600 hover:bg-rose-50 rounded-lg font-bold transition-colors text-left"
                      >
                        <LogOut className="w-4 h-4 mr-2 text-rose-500" />
                        Logout Session
                      </button>
                    </div>
                  </>
                )}
              </div>
            ) : (
              <Button asChild className="bg-[#4285F4] hover:bg-[#3273DC] text-white font-bold rounded-xl h-10 px-5 text-sm shadow-sm">
                <Link to="/login">Login</Link>
              </Button>
            )}

            {/* Mobile Nav Menu Drawer Toggle */}
            <Button
              variant="ghost"
              size="icon"
              onClick={() => setMobileMenuOpen(true)}
              className="md:hidden h-10 w-10 text-neutral-600 hover:bg-neutral-100 rounded-xl"
            >
              <Menu className="w-6 h-6" />
            </Button>
          </div>
        </div>
      </header>

      {/* --- MOBILE DRAWER DIALOG PANEL (Sheet) --- */}
      {mobileMenuOpen && (
        <div className="fixed inset-0 z-50 md:hidden animate-in fade-in duration-200">
          <div 
            className="fixed inset-0 bg-black/40 backdrop-blur-xs" 
            onClick={() => setMobileMenuOpen(false)} 
          />
          <div className="fixed top-0 right-0 bottom-0 w-3/4 max-w-sm bg-white border-l border-neutral-200 p-6 shadow-2xl flex flex-col justify-between z-50 animate-in slide-in-from-right duration-250">
            <div>
              <div className="flex items-center justify-between pb-6 border-b border-neutral-100">
                <span className="font-bold text-lg text-neutral-900 flex items-center gap-2">
                  <BookOpen className="w-5 h-5 text-[#4285F4]" /> Menu
                </span>
                <Button
                  variant="ghost"
                  size="icon"
                  onClick={() => setMobileMenuOpen(false)}
                  className="h-9 w-9 text-neutral-500 hover:bg-neutral-100 rounded-lg"
                >
                  <X className="w-5 h-5" />
                </Button>
              </div>

              {/* Mobile Drawer Menu Links */}
              <nav className="flex flex-col gap-4 pt-6">
                {activeMenuItems.map((item) => (
                  <Link
                    key={item.label}
                    to={item.path}
                    onClick={() => setMobileMenuOpen(false)}
                    className="flex items-center text-base font-bold text-neutral-700 hover:text-[#4285F4] py-2 transition-colors"
                  >
                    {item.icon}
                    {item.label}
                  </Link>
                ))}
              </nav>
            </div>

            {/* Mobile Drawer Session Footer */}
            {user && (
              <div className="border-t border-neutral-100 pt-6">
                <div className="flex items-center gap-3 mb-4">
                  <div className="w-9 h-9 bg-[#4285F4]/10 rounded-full flex items-center justify-center text-[#4285F4] font-bold text-sm border border-[#4285F4]/20 uppercase">
                    {user.fullName ? user.fullName[0] : 'U'}
                  </div>
                  <div>
                    <p className="text-sm font-bold text-neutral-800">{user.fullName}</p>
                    <p className="text-xs text-neutral-500 truncate max-w-[200px]">{user.email}</p>
                  </div>
                </div>
                <Button 
                  onClick={() => {
                    setMobileMenuOpen(false)
                    handleLogout()
                  }}
                  variant="destructive" 
                  className="w-full font-bold py-5 rounded-xl text-sm"
                >
                  <LogOut className="w-4 h-4 mr-2" /> Logout Session
                </Button>
              </div>
            )}
          </div>
        </div>
      )}

      {/* --- MAIN PAGE COMPONENT RENDER AREA (No Sidebar) --- */}
      <main className="flex-1 w-full max-w-6xl mx-auto px-4 py-8">
        <Outlet />
      </main>

      {/* --- ADMIN FOOTER AREA --- */}
      <footer className="w-full bg-[#212833] text-neutral-400 py-12 border-t border-neutral-800 mt-auto">
        <div className="container mx-auto px-4 md:px-6">
          <div className="grid grid-cols-1 md:grid-cols-4 gap-8 pb-8 border-b border-neutral-800">
            
            {/* Logo and Subscribe */}
            <div className="space-y-4">
              <Link to="/" className="flex items-center space-x-2 text-white">
                <BookOpen className="w-6 h-6 text-[#4285F4]" />
                <span className="font-extrabold text-xl tracking-tight">Bookjar</span>
              </Link>
              <p className="text-xs leading-normal">
                Governance, identity, and access management services.
              </p>
            </div>

            {/* Links Columns */}
            <div>
              <h4 className="text-sm font-bold uppercase tracking-wider text-white mb-4">Company</h4>
              <ul className="space-y-2 text-xs">
                <li><Link to="/about" className="hover:text-white transition-colors">About Us</Link></li>
                <li><Link to="/contact" className="hover:text-white transition-colors">Contact Us</Link></li>
                <li><Link to="/privacy-policy" className="hover:text-white transition-colors">Privacy Policy</Link></li>
              </ul>
            </div>

            <div>
              <h4 className="text-sm font-bold uppercase tracking-wider text-white mb-4">Services</h4>
              <ul className="space-y-2 text-xs">
                <li><Link to={user?.roles.includes('Admin') ? '/admin' : '/'} className="hover:text-white transition-colors">Dashboard</Link></li>
                <li><Link to="/profile" className="hover:text-white transition-colors">User Profile</Link></li>
                {hasPermission('Permission.Identity.Users.Read') && (
                  <li><Link to="/admin/users" className="hover:text-white transition-colors">User Management</Link></li>
                )}
                {hasPermission('Permission.Product.Categories.Read') && (
                  <li><Link to="/admin/categories" className="hover:text-white transition-colors">Categories Management</Link></li>
                )}
                {hasPermission('Permission.Identity.AuditTrails.Read') && (
                  <li><Link to="/admin/audit-logs" className="hover:text-white transition-colors">Audit Logs</Link></li>
                )}
              </ul>
            </div>

            <div>
              <h4 className="text-sm font-bold uppercase tracking-wider text-white mb-4">Contact Us</h4>
              <ul className="space-y-2 text-xs">
                <li className="flex items-center gap-2">
                  <span>Phone:</span>
                  <span className="text-neutral-300 font-medium">+61 (0) 3 8376 6284</span>
                </li>
                <li className="flex items-center gap-2">
                  <span>Email:</span>
                  <a href="mailto:noreply@bookjar.com" className="text-[#4285F4] hover:underline">noreply@bookjar.com</a>
                </li>
              </ul>
            </div>
            
          </div>

          <div className="flex flex-col sm:flex-row justify-between items-center pt-8 text-xs gap-4 text-center sm:text-left">
            <p>Â© 2026 Bookjar. All Rights Reserved.</p>
            <div className="flex gap-4">
              <span className="hover:text-white cursor-pointer transition-colors">Facebook</span>
              <span className="hover:text-white cursor-pointer transition-colors">Twitter</span>
              <span className="hover:text-white cursor-pointer transition-colors">GitHub</span>
            </div>
          </div>
        </div>
      </footer>

    </div>
  )
}