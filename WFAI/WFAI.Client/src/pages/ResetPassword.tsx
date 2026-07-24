import React, { useState, useEffect } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { Eye, EyeOff, Loader2, CheckCircle2, XCircle, ShieldAlert, ArrowRight } from 'lucide-react'
import { api } from '../lib/api-client'
import { useToast } from '../components/ui/toast'

export default function ResetPassword() {
  const navigate = useNavigate()
  const toast = useToast()
  const [searchParams] = useSearchParams()
  const emailParam = searchParams.get('email')
  const tokenParam = searchParams.get('token')

  const [password, setPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const [showConfirmPassword, setShowConfirmPassword] = useState(false)

  // API states
  const [isLoading, setIsLoading] = useState(false)
  const [status, setStatus] = useState<'idle' | 'success' | 'invalid-link'>('idle')
  const [countdown, setCountdown] = useState(3)

  // Form touched states
  const [touched, setTouched] = useState({
    password: false,
    confirmPassword: false,
  })

  // Ensure link parameters are present on load
  useEffect(() => {
    if (!emailParam || !tokenParam) {
      setStatus('invalid-link')
    }
  }, [emailParam, tokenParam])

  // Password strength calculation matching Register.tsx
  const getPasswordStrength = (pass: string) => {
    if (!pass) return { label: 'None', color: 'bg-neutral-200' }

    const hasMinLength = pass.length >= 6
    const hasUpper = /[A-Z]/.test(pass)
    const hasLower = /[a-z]/.test(pass)
    const hasNumber = /\d/.test(pass)
    const hasSpecial = /[^a-zA-Z0-9]/.test(pass)

    if (!hasMinLength || !hasUpper || !hasLower || !hasNumber || !hasSpecial) {
      return { label: 'Weak', color: 'bg-rose-500' }
    }
    return { label: 'Strong', color: 'bg-emerald-500' }
  }

  const strength = getPasswordStrength(password)

  // Form validity check
  const isFormValid =
    emailParam &&
    tokenParam &&
    strength.label === 'Strong' &&
    password === confirmPassword

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()

    if (!isFormValid) {
      toast.error('Please fix the validation errors before submitting.')
      return
    }

    setIsLoading(true)

    try {
      const result = await api.post('api/v1/account/reset-password', {
        email: emailParam,
        token: tokenParam,
        password,
        confirmPassword,
      })

      if (result.isSuccessful) {
        setStatus('success')
        toast.success('Your password has been changed successfully!')
        
        // Start countdown to redirect
        const interval = setInterval(() => {
          setCountdown((prev) => {
            if (prev <= 1) {
              clearInterval(interval)
              navigate('/login')
            }
            return prev - 1
          })
        }, 1000)

        return () => clearInterval(interval)
      } else {
        const errors = result.messages || ['Failed to reset password.']
        toast.error(errors.join(' '))
      }
    } catch (err) {
      console.error(err)
      toast.error('Failed to connect to the server. Please verify the backend is running.')
    } finally {
      setIsLoading(false)
    }
  }

  return (
    <div
      className="relative min-h-screen flex items-center justify-center bg-cover bg-center bg-no-repeat px-4 py-8"
      style={{ backgroundImage: "url('/assets/img/login/login-bg.png')" }}
    >
      {/* Background overlay for depth */}
      <div className="absolute inset-0 bg-[#4285f4]/5 backdrop-blur-[2px]" />

      {/* Main glassmorphic card container */}
      <div className="relative z-10 w-full max-w-[500px] bg-white/80 backdrop-blur-xl rounded-[24px] shadow-[0_20px_50px_rgba(0,0,0,0.08)] border border-white/20 p-8 md:p-10 flex flex-col items-center text-center transition-all duration-300">
        
        {/* Brand Logo */}
        <div className="mb-8">
          <Link to="/">
            <img src="/assets/img/home-two/logo-dark.svg" alt="Bookjar Logo" className="h-8" />
          </Link>
        </div>

        {status === 'invalid-link' && (
          <div className="w-full flex flex-col items-center space-y-6 my-4 animate-in fade-in zoom-in-95 duration-500">
            <div className="flex items-center justify-center w-16 h-16 bg-red-50 rounded-full border border-red-100 shadow-[0_8px_16px_rgba(239,68,68,0.08)]">
              <XCircle className="w-8 h-8 text-red-500" />
            </div>
            <div>
              <h3 className="text-xl font-bold text-neutral-800">Invalid Link</h3>
              <p className="text-neutral-500 text-xs mt-2 px-4 leading-relaxed">
                The password reset link is invalid or missing required parameters. Please request a new link.
              </p>
            </div>
            <Link
              to="/forgot-password"
              className="px-6 py-2.5 bg-[#4285F4] hover:bg-[#3273DC] text-white text-xs font-bold rounded-xl transition-all flex items-center gap-1.5 shadow-[0_4px_12px_rgba(66,133,244,0.2)]"
            >
              Request New Link <ArrowRight className="w-3.5 h-3.5" />
            </Link>
          </div>
        )}

        {status === 'success' && (
          <div className="w-full flex flex-col items-center space-y-6 my-4 animate-in fade-in zoom-in-95 duration-500">
            <div className="flex items-center justify-center w-20 h-20 bg-emerald-50 rounded-full border border-emerald-100 shadow-[0_8px_16px_rgba(16,185,129,0.1)]">
              <CheckCircle2 className="w-10 h-10 text-emerald-500 animate-bounce" />
            </div>
            <div>
              <h3 className="text-2xl font-bold text-neutral-800">Password Reset!</h3>
              <p className="text-emerald-600 text-sm font-medium mt-1">Your password has been changed successfully.</p>
              <p className="text-neutral-500 text-xs mt-4">
                Redirecting you to the login screen in <span className="font-bold text-[#4285F4]">{countdown}</span> seconds...
              </p>
            </div>
            <Link
              to="/login"
              className="px-6 py-2.5 bg-[#4285F4] hover:bg-[#3273DC] text-white text-xs font-bold rounded-xl transition-all flex items-center gap-1.5 shadow-[0_4px_12px_rgba(66,133,244,0.2)]"
            >
              Go to Login <ArrowRight className="w-3.5 h-3.5" />
            </Link>
          </div>
        )}

        {status === 'idle' && (
          <div className="w-full flex flex-col items-center space-y-6 my-4">
            <div className="flex items-center justify-center w-16 h-16 bg-[#4285F4]/10 rounded-full">
              <ShieldAlert className="w-8 h-8 text-[#4285F4]" />
            </div>

            <div className="text-center">
              <h3 className="text-xl font-bold text-neutral-800">Reset Your Password</h3>
              <p className="text-neutral-500 text-xs mt-2 px-4 leading-relaxed">
                Please enter a new strong password below to complete the reset.
              </p>
            </div>

            <form onSubmit={handleSubmit} className="w-full space-y-4 text-left">
              
              {/* Email (Disabled Display) */}
              <div>
                <label className="block text-xs font-semibold text-neutral-500 mb-1.5 pl-1">Email Account</label>
                <input
                  type="text"
                  value={emailParam || ''}
                  disabled
                  className="w-full px-4 py-2.5 bg-neutral-100/80 border border-neutral-200 rounded-xl text-neutral-500 text-sm cursor-not-allowed select-none focus:outline-none"
                />
              </div>

              {/* Password Input */}
              <div className="relative">
                <label className="block text-xs font-semibold text-neutral-500 mb-1.5 pl-1">New Password</label>
                <input
                  type={showPassword ? 'text' : 'password'}
                  placeholder="Enter new password"
                  value={password}
                  onChange={(e) => {
                    setPassword(e.target.value)
                    setTouched((prev) => ({ ...prev, password: true }))
                  }}
                  required
                  className={`w-full px-4 py-3 pr-11 bg-white border ${touched.password && strength.label === 'Weak' ? 'border-rose-500 focus:border-rose-500' : 'border-neutral-200'} rounded-xl focus:outline-none focus:ring-2 focus:ring-[#4285F4] focus:border-transparent text-sm transition-all placeholder-neutral-400`}
                />
                <button
                  type="button"
                  onClick={() => setShowPassword(!showPassword)}
                  className="absolute right-3.5 bottom-3.5 text-neutral-400 hover:text-neutral-600 transition-colors"
                >
                  {showPassword ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
                </button>
              </div>

              {/* Password Strength Meter */}
              {password && (
                <div className="space-y-1.5 px-1">
                  <div className="flex items-center justify-between text-xs text-neutral-500">
                    <span>Password Strength:</span>
                    <span className={`font-bold ${
                      strength.label === 'Strong' ? 'text-emerald-600' : 'text-rose-600'
                    }`}>{strength.label}</span>
                  </div>
                  <div className="flex gap-1 h-1.5 w-full bg-neutral-100 rounded-full overflow-hidden">
                    <div className={`h-full transition-all duration-300 rounded-full ${
                      strength.label === 'Strong' ? 'w-full bg-emerald-500' : 'w-1/3 bg-rose-500'
                    }`} />
                  </div>
                  {strength.label === 'Weak' && (
                    <p className="text-rose-500 text-[11px] leading-tight">
                      Password is too weak. It must be at least 8 characters and contain mixed case, numbers, and symbols.
                    </p>
                  )}
                </div>
              )}

              {/* Confirm Password Input */}
              <div className="relative">
                <label className="block text-xs font-semibold text-neutral-500 mb-1.5 pl-1">Confirm Password</label>
                <input
                  type={showConfirmPassword ? 'text' : 'password'}
                  placeholder="Confirm new password"
                  value={confirmPassword}
                  onChange={(e) => {
                    setConfirmPassword(e.target.value)
                    setTouched((prev) => ({ ...prev, confirmPassword: true }))
                  }}
                  required
                  className={`w-full px-4 py-3 pr-11 bg-white border ${touched.confirmPassword && password !== confirmPassword ? 'border-rose-500 focus:border-rose-500' : 'border-neutral-200'} rounded-xl focus:outline-none focus:ring-2 focus:ring-[#4285F4] focus:border-transparent text-sm transition-all placeholder-neutral-400`}
                />
                <button
                  type="button"
                  onClick={() => setShowConfirmPassword(!showConfirmPassword)}
                  className="absolute right-3.5 bottom-3.5 text-neutral-400 hover:text-neutral-600 transition-colors"
                >
                  {showConfirmPassword ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
                </button>
                {touched.confirmPassword && password !== confirmPassword && (
                  <p className="text-rose-500 text-xs mt-1 pl-1">Passwords do not match.</p>
                )}
              </div>

              <button
                type="submit"
                disabled={isLoading || !isFormValid}
                className="w-full py-3.5 bg-[#4285F4] hover:bg-[#3273DC] disabled:opacity-50 disabled:cursor-not-allowed text-white text-sm font-bold rounded-xl shadow-[0_4px_12px_rgba(66,133,244,0.2)] transition-all flex items-center justify-center gap-2"
              >
                {isLoading ? (
                  <>
                    <Loader2 className="w-4 h-4 animate-spin" />
                    Resetting Password...
                  </>
                ) : (
                  'Reset Password'
                )}
              </button>
            </form>
          </div>
        )}
      </div>
    </div>
  )
}