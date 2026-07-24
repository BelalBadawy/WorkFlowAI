import { Outlet } from 'react-router-dom'

export default function LoginLayout() {
  return (
    <div className="min-h-screen w-full bg-background flex flex-col justify-between">
      <main className="flex-grow flex flex-col">
        <Outlet />
      </main>
    </div>
  )
}