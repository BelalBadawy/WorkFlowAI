import React, { useEffect, useState } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { CheckCircle2, XCircle, Loader2, Mail, ArrowRight } from 'lucide-react'
import { api } from '../lib/api-client'
import { useToast } from '../components/ui/toast'

export default function ConfirmEmail() {
  const navigate = useNavigate()
  const toast = useToast()
  const [searchParams] = useSearchParams()
  const userId = searchParams.get('userId')
  const token = searchParams.get('token')

  const [status, setStatus] = useState<'loading' | 'success' | 'error'>('loading')
  const [errorMsg, setErrorMsg] = useState<string | null>(null)
  
  // Resend form states
  const [email, setEmail] = useState('')
  const [isResending, setIsResending] = useState(false)
  const [touched, setTouched] = useState(false)
  const [countdown, setCountdown] = useState(3)

  const isEmailValid = /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)

  useEffect(() => {
    let isMounted = true

    const confirm = async () => {
      if (!userId || !token) {
        setStatus('error')
        setErrorMsg('Invalid or missing confirmation link parameters.')
        return
      }

      try {
        const result = await api.post('api/v1/account/confirm-email', {
          userId: parseInt(userId, 10),
          token,
        })

        if (!isMounted) return

        if (result.isSuccessful) {
          setStatus('success')
          toast.success('Email confirmed successfully!')
          
          // Countdown timer for redirection
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
          setStatus('error')
          const messages = result.messages || ['Email confirmation failed.']
          setErrorMsg(messages.join(' '))
          toast.error('Email confirmation failed.')
        }
      } catch (err) {
        if (!isMounted) return
        setStatus('error')
        setErrorMsg('Failed to connect to the server. Please verify the backend is running.')
      }
    }

    confirm()

    return () => {
      isMounted = false
    }
  }, [userId, token, navigate])

  const handleResend = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!isEmailValid) {
      toast.error('Please enter a valid email address.')
      return
    }

    setIsResending(true)

    try {
      const result = await api.post('api/v1/account/resend-confirmation-email', {
        email,
      })

      if (result.isSuccessful) {
        toast.success(result.messages?.[0] || 'Verification link resent! Please check your inbox.')
        setEmail('')
        setTouched(false)
      } else {
        toast.error(result.messages?.[0] || 'Failed to resend confirmation email.')
      }
    } catch (err) {
      toast.error('Failed to connect to the server.')
    } finally {
      setIsResending(false)
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

        {status === 'loading' && (
          <div className="flex flex-col items-center space-y-6 my-6">
            <div className="relative flex items-center justify-center w-20 h-20">
              <div className="absolute inset-0 rounded-full border-4 border-[#4285F4]/10 animate-pulse" />
              <div className="absolute inset-0 rounded-full border-4 border-t-[#4285F4] animate-spin" style={{ animationDuration: '1.2s' }} />
              <Loader2 className="w-8 h-8 text-[#4285F4] animate-pulse" />
            </div>
            <div>
              <h3 className="text-xl font-bold text-neutral-800">Verifying Your Email</h3>
              <p className="text-neutral-500 text-sm mt-2">Checking verification token. Please wait...</p>
            </div>
          </div>
        )}

        {status === 'success' && (
          <div className="flex flex-col items-center space-y-6 my-6 animate-in fade-in zoom-in-95 duration-500">
            <div className="flex items-center justify-center w-20 h-20 bg-emerald-50 rounded-full border border-emerald-100 shadow-[0_8px_16px_rgba(16,185,129,0.1)]">
              <CheckCircle2 className="w-10 h-10 text-emerald-500 animate-bounce" />
            </div>
            <div>
              <h3 className="text-2xl font-bold text-neutral-800">Email Confirmed!</h3>
              <p className="text-emerald-600 text-sm font-medium mt-1">Thank you for verifying your email address.</p>
              <p className="text-neutral-500 text-xs mt-4">
                Redirecting you to the login screen in <span className="font-bold text-[#4285F4]">{countdown}</span> seconds...
              </p>
            </div>
            <Link
              to="/login"
              className="mt-4 px-6 py-2.5 bg-[#4285F4] hover:bg-[#3273DC] text-white text-xs font-bold rounded-xl transition-all flex items-center gap-1.5 shadow-[0_4px_12px_rgba(66,133,244,0.2)]"
            >
              Go to Login <ArrowRight className="w-3.5 h-3.5" />
            </Link>
          </div>
        )}

        {status === 'error' && (
          <div className="w-full flex flex-col items-center space-y-6 my-4 animate-in fade-in zoom-in-95 duration-500">
            <div className="flex items-center justify-center w-16 h-16 bg-red-50 rounded-full border border-red-100 shadow-[0_8px_16px_rgba(239,68,68,0.08)]">
              <XCircle className="w-8 h-8 text-red-500" />
            </div>
            <div>
              <h3 className="text-xl font-bold text-neutral-800">Verification Failed</h3>
              <p className="text-neutral-500 text-xs mt-2 px-4 leading-relaxed">{errorMsg}</p>
            </div>

            {/* Inline Resend Form */}
            <div className="w-full bg-neutral-50/70 border border-neutral-100 rounded-2xl p-5 mt-4 text-left">
              <h4 className="text-xs font-bold text-neutral-700 uppercase tracking-wider mb-3 flex items-center gap-1.5">
                <Mail className="w-3.5 h-3.5 text-neutral-500" /> Need a new link?
              </h4>
              <p className="text-neutral-500 text-xs mb-4">
                Enter your email address and we'll send you another verification link.
              </p>

              <form onSubmit={handleResend} className="flex flex-col gap-2">
                <div className="flex gap-2">
                  <input
                    type="email"
                    placeholder="name@example.com"
                    value={email}
                    onChange={(e) => {
                      setEmail(e.target.value)
                      setTouched(true)
                    }}
                    required
                    className="flex-grow min-w-0 px-3.5 py-2.5 bg-white border border-neutral-200 rounded-xl focus:outline-none focus:ring-2 focus:ring-[#4285F4] focus:border-transparent text-xs transition-all placeholder-neutral-400"
                  />
                  <button
                    type="submit"
                    disabled={isResending || !isEmailValid}
                    className="px-4 py-2.5 bg-[#4285F4] hover:bg-[#3273DC] disabled:opacity-50 disabled:cursor-not-allowed text-white text-xs font-bold rounded-xl shadow-[0_4px_10px_rgba(66,133,244,0.15)] transition-all flex items-center justify-center shrink-0"
                  >
                    {isResending ? <Loader2 className="w-3.5 h-3.5 animate-spin" /> : 'Resend'}
                  </button>
                </div>
                {touched && !isEmailValid && (
                  <p className="text-rose-500 text-[11px] pl-1 animate-in fade-in duration-300">Please enter a valid email address.</p>
                )}
              </form>
            </div>

            <Link
              to="/login"
              className="text-[#4285F4] font-semibold text-xs hover:underline mt-4 inline-block"
            >
              Back to Login
            </Link>
          </div>
        )}
      </div>
    </div>
  )
}