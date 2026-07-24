import React, { useState } from 'react'
import { Link } from 'react-router-dom'
import { Loader2, CheckCircle2, ArrowLeft, KeyRound } from 'lucide-react'
import { api } from '../lib/api-client'
import { useToast } from '../components/ui/toast'

export default function ForgotPassword() {
  const toast = useToast()
  const [email, setEmail] = useState('')
  const [isLoading, setIsLoading] = useState(false)
  const [status, setStatus] = useState<'idle' | 'success' | 'error'>('idle')
  const [touched, setTouched] = useState(false)
  const [successMessage, setSuccessMessage] = useState('')

  const isEmailValid = /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!isEmailValid) {
      toast.error('Please enter a valid email address.')
      return
    }

    setIsLoading(true)
    setStatus('idle')

    try {
      // Endpoint expects email as a query parameter (binds via [AsParameters])
      const result = await api.post(`api/v1/account/forgot-password?email=${encodeURIComponent(email)}`, {})

      if (result.isSuccessful) {
        const msg = result.messages?.[0] || 'If the email is registered, you will receive an email shortly.'
        setSuccessMessage(msg)
        setStatus('success')
        toast.success(msg)
      } else {
        setStatus('error')
        toast.error(result.messages?.[0] || 'Failed to request password reset link.')
      }
    } catch (err) {
      console.error(err)
      setStatus('error')
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

        <div className="w-full flex flex-col items-center space-y-6 my-4">
          <div className="flex items-center justify-center w-16 h-16 bg-[#4285F4]/10 rounded-full">
            <KeyRound className="w-8 h-8 text-[#4285F4]" />
          </div>

          <div className="text-center">
            <h3 className="text-xl font-bold text-neutral-800">Forgot Password</h3>
            <p className="text-neutral-500 text-xs mt-2 px-4 leading-relaxed">
              Enter your email address below, and we will send you a link to reset your password.
            </p>
          </div>

          {status === 'success' ? (
            <div className="w-full p-6 bg-emerald-50 border border-emerald-100 rounded-2xl flex flex-col items-center text-center space-y-4 animate-in fade-in zoom-in-95 duration-500">
              <div className="flex items-center justify-center w-12 h-12 bg-emerald-100 rounded-full text-emerald-600">
                <CheckCircle2 className="w-6 h-6 animate-bounce" />
              </div>
              <div>
                <h4 className="text-sm font-bold text-emerald-800">Check Your Inbox</h4>
                <p className="text-xs text-emerald-600 mt-1.5 leading-relaxed">
                  {successMessage}
                </p>
              </div>
              <Link
                to="/login"
                className="px-5 py-2 bg-emerald-600 hover:bg-emerald-700 text-white text-xs font-bold rounded-xl transition-all shadow-[0_4px_10px_rgba(16,185,129,0.15)]"
              >
                Return to Login
              </Link>
            </div>
          ) : (
            <form onSubmit={handleSubmit} className="w-full space-y-4">
              <div>
                <input
                  type="email"
                  placeholder="name@example.com"
                  value={email}
                  onChange={(e) => {
                    setEmail(e.target.value)
                    setTouched(true)
                  }}
                  required
                  className="w-full px-4 py-3 bg-white border border-neutral-200 rounded-xl focus:outline-none focus:ring-2 focus:ring-[#4285F4] focus:border-transparent text-sm transition-all placeholder-neutral-400"
                />
                {touched && !isEmailValid && (
                  <p className="text-rose-500 text-xs mt-1 pl-1 text-left animate-in fade-in duration-300">Please enter a valid email address.</p>
                )}
              </div>

              <button
                type="submit"
                disabled={isLoading || !isEmailValid}
                className="w-full py-3.5 bg-[#4285F4] hover:bg-[#3273DC] disabled:opacity-50 disabled:cursor-not-allowed text-white text-sm font-bold rounded-xl shadow-[0_4px_12px_rgba(66,133,244,0.2)] transition-all flex items-center justify-center gap-2"
              >
                {isLoading ? (
                  <>
                    <Loader2 className="w-4 h-4 animate-spin" />
                    Sending Request...
                  </>
                ) : (
                  'Send Reset Link'
                )}
              </button>
            </form>
          )}

          <div className="pt-2">
            <Link
              to="/login"
              className="text-neutral-500 hover:text-neutral-700 font-semibold text-xs transition-colors flex items-center gap-1.5"
            >
              <ArrowLeft className="w-3.5 h-3.5" /> Back to Login
            </Link>
          </div>
        </div>
      </div>
    </div>
  )
}