import { useEffect, useState } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { CheckCircle2, XCircle, Loader2, ArrowRight } from 'lucide-react'
import { api } from '../lib/api-client'
import { useToast } from '../components/ui/toast'

export default function ConfirmEmailChange() {
  const navigate = useNavigate()
  const toast = useToast()
  const [searchParams] = useSearchParams()
  const userId = searchParams.get('userId')
  const newEmail = searchParams.get('newEmail')
  const token = searchParams.get('token')

  const [status, setStatus] = useState<'loading' | 'success' | 'error'>('loading')
  const [errorMsg, setErrorMsg] = useState<string | null>(null)
  const [countdown, setCountdown] = useState(3)

  useEffect(() => {
    let isMounted = true

    const confirm = async () => {
      if (!userId || !newEmail || !token) {
        setStatus('error')
        const msg = 'Invalid or missing confirmation parameters.'
        setErrorMsg(msg)
        toast.error(msg)
        return
      }

      try {
        const result = await api.post('api/v1/account/confirm-email-change', {
          userId: parseInt(userId, 10),
          newEmail,
          token,
        })

        if (!isMounted) return

        if (result.isSuccessful) {
          setStatus('success')
          toast.success(result.messages?.[0] || 'Email changed successfully!')
          
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
          const messages = result.messages || ['Email change confirmation failed.']
          const msgStr = messages.join(' ')
          setErrorMsg(msgStr)
          toast.error(msgStr)
        }
      } catch (err) {
        if (!isMounted) return
        setStatus('error')
        const msg = 'Failed to connect to the server. Please verify the backend is running.'
        setErrorMsg(msg)
        toast.error(msg)
      }
    }

    confirm()

    return () => {
      isMounted = false
    }
  }, [userId, newEmail, token, navigate])

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
              <h3 className="text-xl font-bold text-neutral-800">Confirming Email Change</h3>
              <p className="text-neutral-500 text-sm mt-2">Updating your registered email address. Please wait...</p>
            </div>
          </div>
        )}

        {status === 'success' && (
          <div className="flex flex-col items-center space-y-6 my-6 animate-in fade-in zoom-in-95 duration-500">
            <div className="flex items-center justify-center w-20 h-20 bg-emerald-50 rounded-full border border-emerald-100 shadow-[0_8px_16px_rgba(16,185,129,0.1)]">
              <CheckCircle2 className="w-10 h-10 text-emerald-500 animate-bounce" />
            </div>
            <div>
              <h3 className="text-2xl font-bold text-neutral-800">Email Changed!</h3>
              <p className="text-emerald-600 text-sm font-medium mt-1">Your registered email has been successfully updated.</p>
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
              <h3 className="text-xl font-bold text-neutral-800">Confirmation Failed</h3>
              <p className="text-neutral-500 text-xs mt-2 px-4 leading-relaxed">{errorMsg}</p>
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