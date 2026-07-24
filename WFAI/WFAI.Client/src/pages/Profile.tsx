import React, { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { Shield, ShieldAlert, ShieldCheck, User, Key, Eye, EyeOff, Loader2, ArrowLeft, Copy, Check, Download, AlertTriangle } from 'lucide-react'
import { api } from '../lib/api-client'
import { useToast } from '../components/ui/toast'
import { useAuth } from '../components/AuthContext'
import { Button } from '../components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle, CardFooter } from '../components/ui/card'
import { Badge } from '../components/ui/badge'
import { QRCodeSVG } from 'qrcode.react'

interface ProfileData {
  id: number
  fullName: string
  email: string
  userName: string
  isActive: boolean
  emailConfirmed: boolean
  phoneNumber: string | null
  twoFactorEnabled: boolean
  createdDate: string
  roles: string[]
  permissions: string[]
}

interface TwoFactorSetupResponse {
  keySecret: string
  codeQR: string
}

export default function Profile() {
  const toast = useToast()
  const { user } = useAuth()
  const isAdmin = user?.roles.includes('Admin')

  // Component states
  const [profile, setProfile] = useState<ProfileData | null>(null)
  const [loading, setLoading] = useState(true)

  // Modals visibility
  const [showSetupModal, setShowSetupModal] = useState(false)
  const [showDisableModal, setShowDisableModal] = useState(false)

  // Setup Wizard states
  const [setupStep, setSetupStep] = useState(1) // 1: QR & Secret, 2: Code input, 3: Recovery codes
  const [setupData, setSetupData] = useState<TwoFactorSetupResponse | null>(null)
  const [setupCode, setSetupCode] = useState('')
  const [recoveryCodes, setRecoveryCodes] = useState<string[]>([])
  const [copiedCodes, setCopiedCodes] = useState(false)
  const [setupLoading, setSetupLoading] = useState(false)
  const [setupError, setSetupError] = useState('')

  // Disable states
  const [disablePassword, setDisablePassword] = useState('')
  const [disableCode, setDisableCode] = useState('')
  const [showDisablePassword, setShowDisablePassword] = useState(false)
  const [disableLoading, setDisableLoading] = useState(false)
  const [disableError, setDisableError] = useState('')

  // Clipboard support
  const [copiedKey, setCopiedKey] = useState(false)

  const fetchProfile = async () => {
    setLoading(true)
    try {
      const response = await api.get('api/v1/account/profile')
      if (response.isSuccessful && response.data) {
        setProfile(response.data)
      } else {
        toast.error('Failed to load profile data.')
      }
    } catch (err) {
      console.error(err)
      toast.error('Failed to connect to the server.')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    fetchProfile()
  }, [])

  // Start 2FA Setup
  const handleStartSetup = async () => {
    setSetupLoading(true)
    setSetupError('')
    setSetupStep(1)
    setSetupCode('')
    try {
      const response = await api.post('api/v1/users/setup-2fa')
      if (response.isSuccessful && response.data) {
        setSetupData(response.data)
        setShowSetupModal(true)
      } else {
        toast.error(response.messages?.join(' ') || 'Failed to initialize 2FA setup.')
      }
    } catch (err) {
      console.error(err)
      toast.error('An error occurred during setup initialization.')
    } finally {
      setSetupLoading(false)
    }
  }

  // Verify and Enable 2FA
  const handleVerifyAndEnable = async () => {
    if (setupCode.length !== 6) {
      setSetupError('Please enter a 6-digit code.')
      return
    }
    setSetupLoading(true)
    setSetupError('')
    try {
      const response = await api.put('api/v1/users/enable-2fa', { code: setupCode })
      if (response.isSuccessful && response.data) {
        setRecoveryCodes(response.data)
        setSetupStep(3)
        toast.success('Two-Factor Authentication enabled successfully!')
      } else {
        setSetupError(response.messages?.join(' ') || 'Verification failed. Please try again.')
      }
    } catch (err) {
      console.error(err)
      setSetupError('Failed to complete verification.')
    } finally {
      setSetupLoading(false)
    }
  }

  // Disable 2FA
  const handleDisable2FA = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!disablePassword) {
      setDisableError('Password is required.')
      return
    }
    setDisableLoading(true)
    setDisableError('')
    try {
      const response = await api.put('api/v1/users/disable-2fa', {
        password: disablePassword,
        code: disableCode || null
      })
      if (response.isSuccessful) {
        toast.success('Two-Factor Authentication disabled.')
        setShowDisableModal(false)
        setDisablePassword('')
        setDisableCode('')
        fetchProfile()
      } else {
        setDisableError(response.messages?.join(' ') || 'Failed to disable 2FA.')
      }
    } catch (err) {
      console.error(err)
      setDisableError('An error occurred. Please try again.')
    } finally {
      setDisableLoading(false)
    }
  }

  const handleCopyKey = () => {
    if (setupData?.keySecret) {
      navigator.clipboard.writeText(setupData.keySecret)
      setCopiedKey(true)
      setTimeout(() => setCopiedKey(false), 2000)
    }
  }

  const handleCopyCodes = () => {
    if (recoveryCodes.length > 0) {
      navigator.clipboard.writeText(recoveryCodes.join('\n'))
      setCopiedCodes(true)
      setTimeout(() => setCopiedCodes(false), 2000)
    }
  }

  const handleDownloadCodes = () => {
    if (recoveryCodes.length > 0) {
      const element = document.createElement('a')
      const file = new Blob([recoveryCodes.join('\n')], { type: 'text/plain' })
      element.href = URL.createObjectURL(file)
      element.download = '2fa-recovery-codes.txt'
      document.body.appendChild(element)
      element.click()
      document.body.removeChild(element)
    }
  }

  if (loading) {
    return (
      <div className="min-h-screen bg-neutral-50 flex items-center justify-center">
        <div className="flex flex-col items-center gap-3">
          <Loader2 className="w-8 h-8 text-[#4285F4] animate-spin" />
          <p className="text-sm font-semibold text-neutral-500">Loading profile details...</p>
        </div>
      </div>
    )
  }

  return (
    <div className="min-h-screen bg-neutral-100 p-6 flex flex-col items-center">
      <div className="w-full max-w-3xl space-y-6">
        
        {/* Navigation header */}
        <div className="flex items-center justify-between">
          <Link
            to={isAdmin ? '/admin' : '/'}
            className="flex items-center gap-2 text-sm text-neutral-600 hover:text-neutral-900 transition-colors font-medium"
          >
            <ArrowLeft className="w-4 h-4" />
            Back to Dashboard
          </Link>
          <Badge variant="outline" className="text-xs uppercase px-3 py-1 font-bold">
            My Account
          </Badge>
        </div>

        {/* User Card */}
        <Card className="bg-white border-neutral-200 shadow-xl overflow-hidden rounded-2xl">
          <CardHeader className="flex flex-col sm:flex-row items-center gap-5 pb-6 border-b border-neutral-100 bg-neutral-50/50">
            <div className="w-16 h-16 bg-[#4285F4]/10 border border-[#4285F4]/20 rounded-2xl flex items-center justify-center text-[#4285F4]">
              <User className="w-8 h-8" />
            </div>
            <div className="text-center sm:text-left space-y-1">
              <CardTitle className="text-2xl font-bold text-neutral-900">{profile?.fullName}</CardTitle>
              <CardDescription className="text-sm text-neutral-500">{profile?.email}</CardDescription>
              <div className="flex flex-wrap justify-center sm:justify-start gap-1.5 pt-1">
                {profile?.roles.map((role) => (
                  <Badge key={role} className="bg-[#4285F4] hover:bg-[#3273DC] font-semibold text-white">
                    {role}
                  </Badge>
                ))}
              </div>
            </div>
          </CardHeader>

          <CardContent className="p-6 space-y-6">
            
            {/* Account Information Details */}
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              <div className="space-y-1.5">
                <p className="text-xs font-bold text-neutral-400 uppercase tracking-wider">Username</p>
                <p className="text-base text-neutral-800 font-semibold">{profile?.userName}</p>
              </div>
              <div className="space-y-1.5">
                <p className="text-xs font-bold text-neutral-400 uppercase tracking-wider">Phone Number</p>
                <p className="text-base text-neutral-800 font-semibold">{profile?.phoneNumber || 'Not provided'}</p>
              </div>
              <div className="space-y-1.5">
                <p className="text-xs font-bold text-neutral-400 uppercase tracking-wider">Email Verification Status</p>
                <div className="flex items-center gap-1.5 text-sm font-semibold text-emerald-600">
                  <ShieldCheck className="w-4 h-4" />
                  <span>Verified</span>
                </div>
              </div>
              <div className="space-y-1.5">
                <p className="text-xs font-bold text-neutral-400 uppercase tracking-wider">Account Active</p>
                <Badge variant="outline" className="border-emerald-200 bg-emerald-50 text-emerald-700 font-bold px-2 py-0.5 text-xs">
                  Active
                </Badge>
              </div>
            </div>

            {/* 2FA Security Card Panel */}
            <div className="border-t border-neutral-100 pt-6">
              <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 p-5 bg-neutral-50 rounded-xl border border-neutral-150">
                <div className="flex items-start gap-3.5">
                  <div className={`p-2.5 rounded-lg shrink-0 ${profile?.twoFactorEnabled ? 'bg-emerald-50 text-emerald-600 border border-emerald-100' : 'bg-amber-50 text-amber-600 border border-amber-100'}`}>
                    <Shield className="w-6 h-6" />
                  </div>
                  <div className="space-y-1">
                    <h3 className="text-base font-bold text-neutral-900">Two-Factor Authentication (2FA)</h3>
                    <p className="text-xs text-neutral-500 leading-normal max-w-md">
                      2FA adds an extra layer of protection by requiring a temporary 6-digit code from an authenticator app each time you sign in.
                    </p>
                  </div>
                </div>

                <div className="flex items-center sm:self-center">
                  {profile?.twoFactorEnabled ? (
                    <Button 
                      variant="destructive" 
                      onClick={() => setShowDisableModal(true)} 
                      className="px-5 py-2.5 font-bold rounded-xl text-sm"
                    >
                      Disable 2FA
                    </Button>
                  ) : (
                    <Button 
                      onClick={handleStartSetup} 
                      disabled={setupLoading}
                      className="bg-[#4285F4] hover:bg-[#3273DC] text-white font-bold px-5 py-2.5 rounded-xl text-sm shadow-sm"
                    >
                      {setupLoading ? <Loader2 className="w-4 h-4 animate-spin mr-2" /> : null}
                      Setup 2FA
                    </Button>
                  )}
                </div>
              </div>
            </div>

          </CardContent>
        </Card>

      </div>

      {/* --- SHADCN-STYLE 2FA SETUP MODAL DIALOG --- */}
      {showSetupModal && setupData && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-xs animate-in fade-in duration-200">
          <Card className="w-full max-w-lg bg-white border border-neutral-200 shadow-2xl rounded-2xl animate-in zoom-in-95 duration-200 max-h-[90vh] flex flex-col">
            <CardHeader className="border-b border-neutral-100 pb-4">
              <CardTitle className="text-xl font-extrabold text-neutral-900 flex items-center gap-2">
                <Key className="w-5 h-5 text-[#4285F4]" />
                Two-Factor Authenticator Setup
              </CardTitle>
              <CardDescription>
                {setupStep === 1 && 'Step 1: Link your authenticator application'}
                {setupStep === 2 && 'Step 2: Verify code and enable security'}
                {setupStep === 3 && 'Setup Complete: Save your recovery codes'}
              </CardDescription>
            </CardHeader>

            <div className="flex-1 overflow-y-auto p-6 space-y-4">
              {/* Step 1: QR scanning */}
              {setupStep === 1 && (
                <div className="space-y-5 text-center sm:text-left">
                  <p className="text-sm text-neutral-600">
                    Scan this QR code using your authenticator app (Google Authenticator, Microsoft Authenticator, Duo, etc.).
                  </p>
                  
                  {/* QR rendering */}
                  <div className="flex justify-center p-4 bg-neutral-50 border border-neutral-150 rounded-xl w-fit mx-auto shadow-sm">
                    <QRCodeSVG value={setupData.codeQR} size={180} />
                  </div>

                  <div className="space-y-2">
                    <p className="text-xs font-bold text-neutral-400 uppercase tracking-wider">Manual entry secret key</p>
                    <div className="flex items-center gap-2 bg-neutral-50 p-2.5 rounded-xl border border-neutral-200 font-mono text-sm justify-between">
                      <span className="text-neutral-800 break-all select-all font-semibold px-1">{setupData.keySecret}</span>
                      <Button 
                        size="icon" 
                        variant="ghost" 
                        onClick={handleCopyKey} 
                        className="text-neutral-500 hover:text-neutral-700 shrink-0 h-8 w-8"
                      >
                        {copiedKey ? <Check className="w-4 h-4 text-emerald-600" /> : <Copy className="w-4 h-4" />}
                      </Button>
                    </div>
                  </div>
                </div>
              )}

              {/* Step 2: Verification Input */}
              {setupStep === 2 && (
                <div className="space-y-4 text-center sm:text-left">
                  <p className="text-sm text-neutral-600">
                    To complete registration, please enter the temporary 6-digit verification code generated by your authenticator app.
                  </p>

                  <div className="space-y-2">
                    <label className="text-xs font-bold text-neutral-400 uppercase tracking-wider block">Verification Code</label>
                    <input
                      type="text"
                      maxLength={6}
                      placeholder="000000"
                      value={setupCode}
                      onChange={(e) => setSetupCode(e.target.value.replace(/\D/g, ''))}
                      className="w-full text-center tracking-widest text-2xl font-extrabold px-4 py-3 bg-white border border-neutral-200 rounded-xl focus:outline-none focus:ring-2 focus:ring-[#4285F4] focus:border-transparent transition-all placeholder-neutral-300"
                    />
                  </div>

                  {setupError && (
                    <div className="p-3 bg-rose-50 border border-rose-200 rounded-xl text-rose-800 text-xs font-semibold flex items-center gap-2">
                      <AlertTriangle className="w-4 h-4 shrink-0 text-rose-600" />
                      <span>{setupError}</span>
                    </div>
                  )}
                </div>
              )}

              {/* Step 3: Recovery Codes */}
              {setupStep === 3 && (
                <div className="space-y-4">
                  <div className="p-3.5 bg-amber-50 border border-amber-200 rounded-xl flex items-start gap-2.5 text-amber-800 text-xs leading-normal">
                    <AlertTriangle className="w-5 h-5 shrink-0 text-amber-600 mt-0.5" />
                    <div>
                      <span className="font-bold block mb-0.5">Warning: Save your recovery codes</span>
                      Recovery codes are used to sign in to your account if you lose access to your authenticator application. Write them down or download them. They will not be displayed again.
                    </div>
                  </div>

                  <div className="grid grid-cols-2 gap-2 bg-neutral-900 text-neutral-100 p-4 rounded-xl font-mono text-sm shadow-inner">
                    {recoveryCodes.map((code) => (
                      <div key={code} className="text-center py-1 font-semibold select-all hover:bg-neutral-800 rounded transition-colors">{code}</div>
                    ))}
                  </div>

                  <div className="flex gap-2 justify-center pt-1">
                    <Button variant="outline" onClick={handleCopyCodes} className="flex-1 py-5 text-xs font-bold rounded-xl border-neutral-200 hover:bg-neutral-50">
                      {copiedCodes ? <Check className="w-4 h-4 mr-1.5 text-emerald-600" /> : <Copy className="w-4 h-4 mr-1.5" />}
                      Copy Codes
                    </Button>
                    <Button variant="outline" onClick={handleDownloadCodes} className="flex-1 py-5 text-xs font-bold rounded-xl border-neutral-200 hover:bg-neutral-50">
                      <Download className="w-4 h-4 mr-1.5" />
                      Download TXT
                    </Button>
                  </div>
                </div>
              )}
            </div>

            <CardFooter className="border-t border-neutral-100 p-4 flex justify-between gap-3 bg-neutral-50/50 rounded-b-2xl">
              {setupStep === 1 && (
                <>
                  <Button variant="ghost" onClick={() => setShowSetupModal(false)} className="px-5 rounded-xl font-semibold border border-transparent text-neutral-600 hover:bg-neutral-100">
                    Cancel
                  </Button>
                  <Button onClick={() => setSetupStep(2)} className="bg-[#4285F4] hover:bg-[#3273DC] text-white font-bold px-6 rounded-xl">
                    Next
                  </Button>
                </>
              )}

              {setupStep === 2 && (
                <>
                  <Button variant="ghost" onClick={() => setSetupStep(1)} className="px-5 rounded-xl font-semibold text-neutral-600 hover:bg-neutral-100">
                    Back
                  </Button>
                  <Button onClick={handleVerifyAndEnable} disabled={setupLoading || setupCode.length !== 6} className="bg-emerald-600 hover:bg-emerald-700 text-white font-bold px-6 rounded-xl">
                    {setupLoading ? <Loader2 className="w-4 h-4 animate-spin mr-2" /> : null}
                    Verify & Enable
                  </Button>
                </>
              )}

              {setupStep === 3 && (
                <Button 
                  onClick={() => {
                    setShowSetupModal(false)
                    fetchProfile()
                  }} 
                  className="w-full bg-[#4285F4] hover:bg-[#3273DC] text-white font-bold py-5 rounded-xl"
                >
                  I've Saved the Recovery Codes
                </Button>
              )}
            </CardFooter>
          </Card>
        </div>
      )}

      {/* --- SHADCN-STYLE 2FA DISABLE MODAL DIALOG --- */}
      {showDisableModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-xs animate-in fade-in duration-200">
          <Card className="w-full max-w-md bg-white border border-neutral-200 shadow-2xl rounded-2xl animate-in zoom-in-95 duration-200">
            <CardHeader className="border-b border-neutral-100 pb-4">
              <CardTitle className="text-xl font-extrabold text-rose-600 flex items-center gap-2">
                <ShieldAlert className="w-5 h-5" />
                Disable Two-Factor Authentication
              </CardTitle>
              <CardDescription>
                Please verify your identity to turn off account 2FA protection.
              </CardDescription>
            </CardHeader>

            <form onSubmit={handleDisable2FA}>
              <div className="p-6 space-y-4">
                
                <div className="p-3.5 bg-rose-50 border border-rose-200 rounded-xl text-rose-800 text-xs leading-normal">
                  Disabling 2FA will reduce your account's protection level. We recommend keeping it enabled for your security.
                </div>

                <div className="space-y-3">
                  <div className="space-y-1.5">
                    <label className="text-xs font-bold text-neutral-400 uppercase tracking-wider block">Confirm Password</label>
                    <div className="relative">
                      <input
                        type={showDisablePassword ? 'text' : 'password'}
                        placeholder="Enter account password"
                        value={disablePassword}
                        onChange={(e) => setDisablePassword(e.target.value)}
                        required
                        className="w-full px-4 py-2.5 bg-white border border-neutral-200 rounded-xl focus:outline-none focus:ring-2 focus:ring-rose-500 focus:border-transparent text-sm transition-all placeholder-neutral-400"
                      />
                      <button
                        type="button"
                        onClick={() => setShowDisablePassword(!showDisablePassword)}
                        className="absolute right-3.5 top-1/2 -translate-y-1/2 text-neutral-400 hover:text-neutral-600"
                      >
                        {showDisablePassword ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
                      </button>
                    </div>
                  </div>

                  <div className="space-y-1.5">
                    <label className="text-xs font-bold text-neutral-400 uppercase tracking-wider block">Authenticator Code (Optional)</label>
                    <input
                      type="text"
                      maxLength={6}
                      placeholder="000000"
                      value={disableCode}
                      onChange={(e) => setDisableCode(e.target.value.replace(/\D/g, ''))}
                      className="w-full px-4 py-2.5 bg-white border border-neutral-200 rounded-xl focus:outline-none focus:ring-2 focus:ring-rose-500 focus:border-transparent text-sm tracking-widest text-center font-bold placeholder-neutral-350"
                    />
                  </div>
                </div>

                {disableError && (
                  <div className="p-3 bg-rose-50 border border-rose-200 rounded-xl text-rose-800 text-xs font-semibold flex items-center gap-2">
                    <AlertTriangle className="w-4 h-4 shrink-0 text-rose-600" />
                    <span>{disableError}</span>
                  </div>
                )}

              </div>

              <CardFooter className="border-t border-neutral-100 p-4 flex justify-end gap-3 bg-neutral-50/50 rounded-b-2xl">
                <Button 
                  type="button" 
                  variant="ghost" 
                  onClick={() => {
                    setShowDisableModal(false)
                    setDisablePassword('')
                    setDisableCode('')
                    setDisableError('')
                  }} 
                  className="px-5 rounded-xl font-semibold text-neutral-600 hover:bg-neutral-100"
                >
                  Cancel
                </Button>
                <Button 
                  type="submit" 
                  disabled={disableLoading || !disablePassword} 
                  variant="destructive"
                  className="px-6 rounded-xl font-bold"
                >
                  {disableLoading ? <Loader2 className="w-4 h-4 animate-spin mr-2" /> : null}
                  Disable 2FA
                </Button>
              </CardFooter>
            </form>
          </Card>
        </div>
      )}

    </div>
  )
}