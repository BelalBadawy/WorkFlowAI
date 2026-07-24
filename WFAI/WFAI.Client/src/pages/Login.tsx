import React, { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { Eye, EyeOff, AlertCircle, Loader2 } from 'lucide-react'
import { api } from '../lib/api-client'
import { useToast } from '../components/ui/toast'
import { useAuth } from '../components/AuthContext'
import { decodeToken } from '../lib/jwt'

export default function Login() {
  const navigate = useNavigate()
  const toast = useToast()
  const { login } = useAuth()

  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [rememberMe, setRememberMe] = useState(false)
  const [showPassword, setShowPassword] = useState(false)

  // API states
  const [isLoading, setIsLoading] = useState(false)
  const [isEmailUnconfirmed, setIsEmailUnconfirmed] = useState(false)

  // 2FA login states
  const [is2FAView, setIs2FAView] = useState(false)
  const [twoFactorCode, setTwoFactorCode] = useState('')
  const [challengeToken, setChallengeToken] = useState('')
  const [isRecoveryCode, setIsRecoveryCode] = useState(false)

  // Form touched states
  const [touched, setTouched] = useState({
    email: false,
    password: false,
  })

  // Form validity check
  const isFormValid =
    /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email) &&
    password.length > 0

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    
    if (!isFormValid) {
      toast.error('Please fill in all fields correctly.')
      return
    }

    setIsLoading(true)
    setIsEmailUnconfirmed(false)

    try {
      const result = await api.post('api/v1/account/login', {
        email,
        password,
      })

      if (result.isSuccessful) {
        if (result.data?.requiresTwoFactor) {
          setChallengeToken(result.data.twoFactorChallengeToken)
          setIs2FAView(true)
          toast.info('Two-Factor Authentication is required to log in.')
          setIsLoading(false)
          return
        }

        toast.success('Login successful!')
        const token = result.data?.token
        const refreshToken = result.data?.refreshToken
        
        if (token && refreshToken) {
          login(token, refreshToken)
          const decoded = decodeToken(token)
          const isAdmin = decoded?.roles.includes('Admin')

          setTimeout(() => {
            navigate(isAdmin ? '/admin' : '/')
          }, 1500)
        } else {
          toast.error('Token information was missing in server response.')
        }
      } else {
        if (result.statusCode === 403) {
          setIsEmailUnconfirmed(true)
          toast.warning('Email not confirmed. Please verify your email before logging in.')
        } else {
          const errors = result.messages || ['Invalid email or password.']
          toast.error(errors.join(' '))
        }
      }
    } catch (err) {
      console.error(err)
      toast.error('Failed to connect to the server. Please make sure the backend is running.')
    } finally {
      setIsLoading(false)
    }
  }

  const handle2FASubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!twoFactorCode) {
      toast.error('Please enter your verification code.')
      return
    }

    setIsLoading(true)
    try {
      const result = await api.post('api/v1/account/login-2fa', {
        twoFactorChallengeToken: challengeToken,
        code: twoFactorCode
      })

      if (result.isSuccessful && result.data?.token) {
        toast.success('Login successful!')
        const token = result.data.token
        const refreshToken = result.data.refreshToken
        
        login(token, refreshToken)
        const decoded = decodeToken(token)
        const isAdmin = decoded?.roles.includes('Admin')

        setTimeout(() => {
          navigate(isAdmin ? '/admin' : '/')
        }, 1500)
      } else {
        const errors = result.messages || ['Invalid verification code.']
        toast.error(errors.join(' '))
      }
    } catch (err) {
      console.error(err)
      toast.error('Failed to authenticate 2FA code.')
    } finally {
      setIsLoading(false)
    }
  }

  return (
    <div
      className="relative min-h-screen flex items-center justify-center bg-cover bg-center bg-no-repeat px-4 py-8"
      style={{ backgroundImage: "url('/assets/img/login/login-bg.png')" }}
    >
      {/* Main card container */}
      <div className="relative z-10 w-full max-w-[1000px] min-h-[580px] bg-white rounded-[24px] shadow-[0_10px_40px_rgba(0,0,0,0.06)] flex overflow-hidden border border-neutral-100/50">

        {/* Left Side: Form */}
        <div className="relative w-full lg:w-1/2 p-8 md:p-12 flex flex-col justify-center bg-white z-10">

          {/* Logo */}
          <div className="mb-6">
            <Link to="/">
              <img src="/assets/img/home-two/logo-dark.svg" alt="Bookjar" className="h-8" />
            </Link>
          </div>

          {is2FAView ? (
            <>
              <h2 className="text-[28px] font-bold text-[#202124] tracking-tight mb-2">Two-Factor Authentication</h2>
              <p className="text-[#636466] text-sm mb-6">
                {isRecoveryCode 
                  ? 'Enter one of your 8-character backup recovery codes to log in.' 
                  : 'Enter the 6-digit code from your authenticator app.'}
              </p>

              <form onSubmit={handle2FASubmit} className="space-y-4">
                <div>
                  <input
                    type="text"
                    maxLength={isRecoveryCode ? 8 : 6}
                    placeholder={isRecoveryCode ? "Recovery Code" : "000000"}
                    value={twoFactorCode}
                    onChange={(e) => setTwoFactorCode(e.target.value.trim())}
                    required
                    className="w-full text-center tracking-widest text-xl font-bold px-4 py-3 bg-white border border-neutral-200 rounded-xl focus:outline-none focus:ring-2 focus:ring-[#4285F4] focus:border-transparent transition-all placeholder-neutral-300 text-[#202124]"
                  />
                </div>

                <button
                  type="submit"
                  disabled={isLoading || (isRecoveryCode ? twoFactorCode.length < 8 : twoFactorCode.length !== 6)}
                  className="w-full py-3.5 bg-[#4285F4] hover:bg-[#3273DC] text-white text-sm font-bold rounded-xl shadow-[0_4px_12px_rgba(66,133,244,0.2)] transition-all flex items-center justify-center gap-2 disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  {isLoading ? (
                    <>
                      <Loader2 className="w-4 h-4 animate-spin" />
                      Verifying...
                    </>
                  ) : (
                    'Verify Code'
                  )}
                </button>
              </form>

              <div className="mt-5 flex flex-col gap-2.5 text-center text-sm">
                <button
                  type="button"
                  onClick={() => {
                    setIsRecoveryCode(!isRecoveryCode)
                    setTwoFactorCode('')
                  }}
                  className="text-[#4285F4] font-semibold hover:underline bg-transparent border-0 cursor-pointer"
                >
                  {isRecoveryCode ? 'Use authenticator app code' : 'Use a backup recovery code'}
                </button>
                <button
                  type="button"
                  onClick={() => {
                    setIs2FAView(false)
                    setTwoFactorCode('')
                    setChallengeToken('')
                  }}
                  className="text-neutral-500 font-semibold hover:underline text-xs bg-transparent border-0 cursor-pointer"
                >
                  Back to Login
                </button>
              </div>
            </>
          ) : (
            <>
              <h2 className="text-[28px] font-bold text-[#202124] tracking-tight mb-2">Login to Your Account</h2>
              <p className="text-[#636466] text-sm mb-6">Welcome Back! Select Method to login:</p>

              {/* Unconfirmed Email Alert Banner */}
              {isEmailUnconfirmed && (
                <div className="mb-5 p-3.5 bg-amber-50 border border-amber-200 rounded-xl flex items-start gap-2.5 text-amber-800 text-sm animate-in fade-in slide-in-from-top-2 duration-300">
                  <AlertCircle className="w-5 h-5 text-amber-600 shrink-0 mt-0.5" />
                  <div className="flex flex-col gap-1">
                    <span className="font-semibold">Email Not Verified</span>
                    <span className="text-xs text-neutral-600">You must confirm your email before logging in.</span>
                    <Link
                      to="/resend-confirmation"
                      className="text-[#4285F4] hover:text-[#3273DC] font-semibold underline mt-1 text-xs flex items-center gap-1"
                    >
                      Click here to resend verification link
                    </Link>
                  </div>
                </div>
              )}

              {/* Login Form */}
              <form onSubmit={handleSubmit} className="space-y-4">
                <div>
                  <input
                    type="email"
                    placeholder="Email"
                    value={email}
                    onChange={(e) => {
                      setEmail(e.target.value)
                      setTouched((prev) => ({ ...prev, email: true }))
                    }}
                    required
                    className="w-full px-4 py-3 bg-white border border-neutral-200 rounded-xl focus:outline-none focus:ring-2 focus:ring-[#4285F4] focus:border-transparent text-sm transition-all placeholder-neutral-400"
                  />
                  {touched.email && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email) && (
                    <p className="text-rose-500 text-xs mt-1 pl-1">Please enter a valid email address.</p>
                  )}
                </div>

                <div className="relative">
                  <input
                    type={showPassword ? 'text' : 'password'}
                    placeholder="Password"
                    value={password}
                    onChange={(e) => {
                      setPassword(e.target.value)
                      setTouched((prev) => ({ ...prev, password: true }))
                    }}
                    required
                    className="w-full px-4 py-3 pr-11 bg-white border border-neutral-200 rounded-xl focus:outline-none focus:ring-2 focus:ring-[#4285F4] focus:border-transparent text-sm transition-all placeholder-neutral-400"
                  />
                  <button
                    type="button"
                    onClick={() => setShowPassword(!showPassword)}
                    className="absolute right-3.5 top-1/2 -translate-y-1/2 text-neutral-400 hover:text-neutral-600 transition-colors"
                  >
                    {showPassword ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
                  </button>
                </div>
                {touched.password && password.length === 0 && (
                  <p className="text-rose-500 text-xs pl-1">Password is required.</p>
                )}

                <div className="flex items-center justify-between text-xs md:text-sm pt-1">
                  <label className="flex items-center gap-2 cursor-pointer text-[#4D4E50]">
                    <input
                      type="checkbox"
                      checked={rememberMe}
                      onChange={(e) => setRememberMe(e.target.checked)}
                      className="rounded border-neutral-300 text-[#4285F4] focus:ring-[#4285F4] h-4 w-4"
                    />
                    <span>Remember me</span>
                  </label>
                  <Link to="/forgot-password" className="text-[#36383A] font-semibold hover:underline">
                    Forgot Password?
                  </Link>
                </div>

                <button
                  type="submit"
                  disabled={isLoading || !isFormValid}
                  className="w-full py-3.5 bg-[#4285F4] hover:bg-[#3273DC] text-white text-sm font-bold rounded-xl shadow-[0_4px_12px_rgba(66,133,244,0.2)] transition-all flex items-center justify-center gap-2 disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  {isLoading ? (
                    <>
                      <Loader2 className="w-4 h-4 animate-spin" />
                      Logging In...
                    </>
                  ) : (
                    'Log In'
                  )}
                </button>
              </form>

              {/* New User Link */}
              <div className="mt-6 text-center text-sm text-[#4D4E50]">
                New user?{' '}
                <Link to="/register" className="text-[#4285F4] font-semibold hover:underline">
                  Create an account
                </Link>
              </div>
            </>
          )}
        </div>

        {/* Right Side: Solid Blue Image panel */}
        <div className="hidden lg:flex w-1/2 bg-[#4285F4] items-center justify-center p-8 relative overflow-hidden">

          <img
            src="/assets/img/login/login-img.png"
            alt="Authentication Visual"
            className="max-h-[440px] w-auto object-contain hover:scale-102 transition-transform duration-500 relative z-10"
          />
        </div>

      </div>
    </div>
  )
}