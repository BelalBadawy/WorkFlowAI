import { Link } from 'react-router-dom'
import { useAuth } from '../components/AuthContext'
import { LogIn, UserPlus, LogOut, ArrowRight, ShieldCheck, User } from 'lucide-react'
import { Button } from '../components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../components/ui/card'

export default function PublicHome() {
  const { user, isAuthenticated, logout } = useAuth()
  const isAdmin = user?.roles.includes('Admin')

  return (
    <div className="min-h-screen bg-neutral-100 flex items-center justify-center p-6 text-neutral-800">
      <Card className="w-full max-w-xl bg-white border-neutral-200 shadow-xl p-4 md:p-8 animate-in fade-in zoom-in-95 duration-500">
        
        {/* Welcome Area */}
        <CardHeader className="text-center pb-6">
          <div className="w-16 h-16 bg-blue-500/10 border border-blue-500/20 rounded-2xl flex items-center justify-center mx-auto mb-4 text-[#4285F4]">
            <span className="text-2xl font-bold">ðŸ‘‹</span>
          </div>
          <CardDescription className="text-xs font-bold uppercase tracking-widest text-neutral-400">
            Public Access
          </CardDescription>
          <CardTitle className="text-4xl font-extrabold tracking-tight text-neutral-900 mt-1">
            {isAuthenticated ? `Hello, ${user?.fullName || 'User'}!` : 'Hello, Guest!'}
          </CardTitle>
          <CardDescription className="text-sm text-neutral-500 mt-2">
            {isAuthenticated 
              ? `You are logged in as a ${user?.roles.join(', ')}.` 
              : 'Welcome to UserManagement system. Please login or register to continue.'}
          </CardDescription>
        </CardHeader>

        {/* Action Panel */}
        <CardContent className="space-y-4">
          {isAuthenticated ? (
            <div className="flex flex-col gap-3">
              {isAdmin && (
                <Button asChild className="w-full py-6 text-base font-bold bg-[#4285F4] hover:bg-[#3273DC] text-white rounded-xl shadow-lg shadow-blue-500/10">
                  <Link to="/admin" className="flex items-center justify-center gap-2">
                    <ShieldCheck className="w-5 h-5" />
                    Go to Admin Panel
                    <ArrowRight className="w-4 h-4 ml-1" />
                  </Link>
                </Button>
              )}
              <Button asChild className="w-full py-6 text-base font-bold bg-[#4285F4] hover:bg-[#3273DC] text-white rounded-xl shadow-lg shadow-blue-500/10">
                <Link to="/profile" className="flex items-center justify-center gap-2">
                  <User className="w-5 h-5" />
                  View Security & Profile Settings
                  <ArrowRight className="w-4 h-4 ml-1" />
                </Link>
              </Button>
              <Button
                onClick={logout}
                variant="outline"
                className="w-full py-6 text-base font-bold text-neutral-700 hover:bg-neutral-100 border-neutral-200 rounded-xl"
              >
                <LogOut className="w-4 h-4 mr-2" />
                Logout Session
              </Button>
            </div>
          ) : (
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
              <Button asChild className="w-full py-6 text-base font-bold bg-[#4285F4] hover:bg-[#3273DC] text-white rounded-xl shadow-lg shadow-blue-500/10">
                <Link to="/login" className="flex items-center justify-center gap-2">
                  <LogIn className="w-4 h-4" />
                  Log In
                </Link>
              </Button>
              <Button asChild variant="outline" className="w-full py-6 text-base font-bold text-neutral-700 border-neutral-300 hover:bg-neutral-50 rounded-xl">
                <Link to="/register" className="flex items-center justify-center gap-2">
                  <UserPlus className="w-4 h-4" />
                  Create Account
                </Link>
              </Button>
            </div>
          )}
        </CardContent>

      </Card>
    </div>
  )
}