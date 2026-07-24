import React, { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { Eye, EyeOff, Loader2 } from 'lucide-react'
import { api } from '../lib/api-client'
import { useToast } from '../components/ui/toast'

export default function Register() {
  const navigate = useNavigate()
  const toast = useToast()

  const [fullName, setFullName] = useState('')
  const [email, setEmail] = useState('')
  const [phoneNumber, setPhoneNumber] = useState('')
  const [password, setPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [agreePrivacy, setAgreePrivacy] = useState(false)

  const [showPassword, setShowPassword] = useState(false)
  const [showConfirmPassword, setShowConfirmPassword] = useState(false)

  // API states
  const [isLoading, setIsLoading] = useState(false)

  // Form touched states
  const [touched, setTouched] = useState({
    fullName: false,
    email: false,
    phoneNumber: false,
    password: false,
    confirmPassword: false,
  })

  // Password strength calculation
  const getPasswordStrength = (pass: string) => {
    if (!pass) return { label: 'None', color: 'bg-neutral-200' };

    const hasMinLength = pass.length >= 6;
    const hasUpper = /[A-Z]/.test(pass);
    const hasLower = /[a-z]/.test(pass);
    const hasNumber = /\d/.test(pass);
    const hasSpecial = /[^a-zA-Z0-9]/.test(pass);

    if (!hasMinLength || !hasUpper || !hasLower || !hasNumber || !hasSpecial) {
      return { label: 'Weak', color: 'bg-rose-500' };
    }
    // At this point all required rules are satisfied â€“ treat as strong
    return { label: 'Strong', color: 'bg-emerald-500' };
  }

  const strength = getPasswordStrength(password)

  // Form validity check
  const isFormValid =
    fullName.trim().length >= 3 &&
    /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email) &&
    /^[0-9+\s-]{7,15}$/.test(phoneNumber) &&
    strength.label !== 'Weak' &&
    strength.label !== 'None' &&
    password === confirmPassword &&
    agreePrivacy

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()

    if (!isFormValid) {
      toast.error('Please fix the validation errors before submitting.')
      return
    }

    setIsLoading(true)

    try {
      const result = await api.post('api/v1/users/register', {
        fullName,
        email,
        phoneNumber,
        password,
        confirmPassword,
        autoConfirmEmail: false,
        activateUser: true,
      })

      if (result.isSuccessful) {
        toast.success('Account created successfully! Please check your inbox for a confirmation link.')
        setTimeout(() => {
          navigate('/login')
        }, 5000)
      } else {
        const errors = result.messages || ['Registration failed.']
        toast.error(errors.join(' '))
      }
    } catch (err) {
      console.error(err)
      toast.error('Failed to connect to the server. Please make sure the backend is running.')
    } finally {
      setIsLoading(false)
    }
  }

  return (
    <div
      className="relative min-h-screen flex items-center justify-center bg-cover bg-center bg-no-repeat px-4 py-8"
      style={{ backgroundImage: "url('/assets/img/login/reginstration-bg.png')" }}
    >
      {/* Main card container */}
      <div className="relative z-10 w-full max-w-[1000px] min-h-[580px] bg-white rounded-[24px] shadow-[0_10px_40px_rgba(0,0,0,0.06)] flex overflow-hidden border border-neutral-100/50">

        {/* Left Side: Form content area */}
        <div className="relative w-full lg:w-1/2 p-8 md:p-12 flex flex-col justify-center bg-white z-10">

          {/* Brand Logo */}
          <div className="mb-6">
            <Link to="/">
              <img src="/assets/img/home-two/logo-dark.svg" alt="Bookjar Logo" className="h-8" />
            </Link>
          </div>

          <h2 className="text-[28px] font-bold text-[#202124] tracking-tight mb-2">Sign Up to Bookjar</h2>
          <p className="text-[#636466] text-sm mb-6">Create Your Account with Just Few Steps</p>

          {/* Signup Form */}
          <form onSubmit={handleSubmit} className="space-y-4">
            <div>
              <input
                type="text"
                placeholder="Full Name"
                value={fullName}
                onChange={(e) => {
                  setFullName(e.target.value)
                  setTouched((prev) => ({ ...prev, fullName: true }))
                }}
                required
                className={`w-full px-4 py-3 bg-white border ${touched.fullName && fullName.trim().length === 0 ? 'border-rose-500 focus:border-rose-500' : 'border-neutral-200'} rounded-xl focus:outline-none focus:ring-2 focus:ring-[#4285F4] focus:border-transparent text-sm transition-all placeholder-neutral-400`}
              />
              {touched.fullName && fullName.trim().length === 0 && (
                <p className="text-rose-500 text-xs mt-1 pl-1">Full name is required.</p>
              )}
            </div>

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
                className={`w-full px-4 py-3 bg-white border ${touched.email && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email) ? 'border-rose-500 focus:border-rose-500' : 'border-neutral-200'} rounded-xl focus:outline-none focus:ring-2 focus:ring-[#4285F4] focus:border-transparent text-sm transition-all placeholder-neutral-400`}
              />
              {touched.email && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email) && (
                <p className="text-rose-500 text-xs mt-1 pl-1">Please enter a valid email address.</p>
              )}
            </div>

            <div className="flex flex-col">
              <input
                type="text"
                placeholder="Phone Number"
                value={phoneNumber}
                onChange={(e) => {
                  setPhoneNumber(e.target.value)
                  setTouched((prev) => ({ ...prev, phoneNumber: true }))
                }}
                required
                className={
                  `w-full px-4 py-3 bg-white border ${touched.phoneNumber && (!/^[0-9+\-\s]+$/.test(phoneNumber) || phoneNumber.length < 11) ? 'border-red-500 focus:border-red-500' : 'border-neutral-200'} rounded-xl focus:outline-none focus:ring-2 focus:ring-[#4285F4] text-sm transition-all placeholder-neutral-400`
                }
              />
              {touched.phoneNumber && (!/^[0-9+\-\s]+$/.test(phoneNumber) || phoneNumber.length < 11) && (
                <p className="text-rose-500 text-[11px] pl-1 animate-in fade-in duration-300">
                  Phone number must contain only numbers, +, - or spaces and be at least 11 characters.
                </p>
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
                className={`w-full px-4 py-3 pr-11 bg-white border ${touched.password && strength.label === 'Weak' ? 'border-rose-500 focus:border-rose-500' : 'border-neutral-200'} rounded-xl focus:outline-none focus:ring-2 focus:ring-[#4285F4] focus:border-transparent text-sm transition-all placeholder-neutral-400`}
              />
              <button
                type="button"
                onClick={() => setShowPassword(!showPassword)}
                className="absolute right-3.5 top-1/2 -translate-y-1/2 text-neutral-400 hover:text-neutral-600 transition-colors"
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
                    strength.label === 'Strong' ? 'text-emerald-600' :
                    strength.label === 'Medium' ? 'text-amber-600' : 'text-rose-600'
                  }`}>{strength.label}</span>
                </div>
                <div className="flex gap-1 h-1.5 w-full bg-neutral-100 rounded-full overflow-hidden">
                  <div className={`h-full transition-all duration-300 rounded-full ${
                    strength.label === 'Strong' ? 'w-full bg-emerald-500' :
                    strength.label === 'Medium' ? 'w-2/3 bg-amber-500' : 'w-1/3 bg-rose-500'
                  }`} />
                </div>
                {strength.label === 'Weak' && (
                  <p className="text-rose-500 text-[11px] leading-tight">
                    Password is too weak. It must be at least 8 characters and contain mixed case, numbers, and symbols.
                  </p>
                )}
              </div>
            )}

            <div className="relative">
              <input
                type={showConfirmPassword ? 'text' : 'password'}
                placeholder="Confirm Password"
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
                className="absolute right-3.5 top-1/2 -translate-y-1/2 text-neutral-400 hover:text-neutral-600 transition-colors"
              >
                {showConfirmPassword ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
              </button>
              {touched.confirmPassword && password !== confirmPassword && (
                <p className="text-rose-500 text-xs mt-1 pl-1">Passwords do not match.</p>
              )}
            </div>

            <div className="pt-1">
              <label className="flex items-start gap-2.5 cursor-pointer text-xs md:text-sm text-[#4D4E50]">
                <input
                  type="checkbox"
                  checked={agreePrivacy}
                  onChange={(e) => setAgreePrivacy(e.target.checked)}
                  required
                  className="rounded border-neutral-300 text-[#4285F4] focus:ring-[#4285F4] h-4 w-4 mt-0.5 shrink-0"
                />
                <span>
                  I Agreed with the{' '}
                  <Link to="/privacy-policy" className="text-[#4285F4] font-semibold hover:underline">
                    Privacy Policy
                  </Link>
                </span>
              </label>
            </div>

            <button
              type="submit"
              disabled={isLoading || !isFormValid}
              className="w-full py-3.5 bg-[#4285F4] hover:bg-[#3273DC] text-white text-sm font-bold rounded-xl shadow-[0_4px_12px_rgba(66,133,244,0.2)] transition-all flex items-center justify-center gap-2 disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {isLoading ? (
                <>
                  <Loader2 className="w-4 h-4 animate-spin" />
                  Creating Account...
                </>
              ) : (
                'Sign Up'
              )}
            </button>
          </form>

          {/* Already have an account link */}
          <div className="mt-5 text-center text-sm text-[#4D4E50]">
            Already have an account?{' '}
            <Link to="/login" className="text-[#4285F4] font-semibold hover:underline">
              Login Here
            </Link>
          </div>
        </div>

        {/* Right Side: Solid Blue Bottom-aligned Image panel */}
        <div className="hidden lg:flex w-1/2 bg-[#4285F4] items-end justify-center p-8 pt-16 relative overflow-hidden">
          <img
            src="/assets/img/login/reginstration-img.png"
            alt="Registration Illustration"
            className="max-h-[440px] w-auto object-contain hover:scale-102 transition-transform duration-500 mt-auto relative z-10"
          />
        </div>

      </div>
    </div>
  )
}