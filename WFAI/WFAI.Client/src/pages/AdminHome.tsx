import { Link } from 'react-router-dom'
import { useAuth } from '../components/AuthContext'
import { LogOut, Shield, Mail, Phone, Lock, User } from 'lucide-react'
import { Button } from '../components/ui/button'
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from '../components/ui/card'
import { Badge } from '../components/ui/badge'

export default function AdminHome() {
  const { user, logout } = useAuth()

  return (
    <div className="min-h-screen bg-neutral-950 flex items-center justify-center p-6 text-neutral-50">
      <Card className="w-full max-w-2xl bg-neutral-900 border-neutral-800 shadow-2xl p-4 md:p-8 animate-in fade-in zoom-in-95 duration-500 text-neutral-50">
        <CardHeader className="flex flex-row items-center gap-4 space-y-0 pb-6 border-b border-neutral-800">
          <div className="p-3 bg-red-500/10 border border-red-500/20 rounded-2xl text-red-400">
            <Shield className="w-8 h-8" />
          </div>
          <div>
            <CardDescription className="text-xs font-semibold uppercase tracking-widest text-red-400">
              Admin Workspace
            </CardDescription>
            <CardTitle className="text-3xl font-extrabold tracking-tight text-white mt-1">
              Hello Admin!
            </CardTitle>
          </div>
        </CardHeader>

        <CardContent className="space-y-6 pt-6">
          <div className="flex flex-col sm:flex-row justify-between gap-4 pb-6 border-b border-neutral-800">
            <div>
              <p className="text-xs text-neutral-400 uppercase tracking-wider mb-1">User Name</p>
              <p className="text-lg font-bold text-neutral-100">{user?.fullName || 'Administrator'}</p>
            </div>
            <div>
              <p className="text-xs text-neutral-400 uppercase tracking-wider mb-1">Assigned Roles</p>
              <div className="flex flex-wrap gap-1.5 mt-1">
                {user?.roles.map((role) => (
                  <Badge key={role} variant="destructive" className="font-semibold px-2.5 py-0.5">
                    {role}
                  </Badge>
                ))}
              </div>
            </div>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4 text-sm text-neutral-300">
            <div className="flex items-center gap-3">
              <Mail className="w-4 h-4 text-neutral-400" />
              <span>{user?.email}</span>
            </div>
            {user?.phoneNumber && (
              <div className="flex items-center gap-3">
                <Phone className="w-4 h-4 text-neutral-400" />
                <span>{user?.phoneNumber}</span>
              </div>
            )}
          </div>

          {user?.permissions && user.permissions.length > 0 && (
            <div className="pt-4 border-t border-neutral-800">
              <p className="text-xs text-neutral-400 uppercase tracking-wider mb-2 flex items-center gap-1.5">
                <Lock className="w-3.5 h-3.5" /> Direct Permissions
              </p>
              <div className="flex flex-wrap gap-1.5">
                {user.permissions.map((perm) => (
                  <Badge key={perm} variant="secondary" className="px-2 py-0.5 rounded text-xs font-normal">
                    {perm}
                  </Badge>
                ))}
              </div>
            </div>
          )}
        </CardContent>

        <CardFooter className="flex justify-end gap-3 pt-4">
          <Button
            asChild
            variant="outline"
            className="flex items-center gap-2 border-neutral-800 text-white bg-neutral-850 hover:bg-neutral-800 transition-all px-5 py-2 h-10"
          >
            <Link to="/profile">
              <User className="w-4 h-4" />
              My Profile
            </Link>
          </Button>
          <Button
            onClick={logout}
            variant="outline"
            className="flex items-center gap-2 border-neutral-800 text-white bg-neutral-850 hover:bg-neutral-800 transition-all px-5 py-2 h-10"
          >
            <LogOut className="w-4 h-4" />
            Logout Session
          </Button>
        </CardFooter>
      </Card>
    </div>
  )
}