import React from 'react';

interface PasswordStrengthMeterProps {
  password: string;
}

interface StrengthResult {
  label: 'None' | 'Weak' | 'Strong';
  color: string;
}

const evaluateStrength = (pass: string): StrengthResult => {
  if (!pass) return { label: 'None', color: 'bg-neutral-200' };

  const hasMinLength = pass.length >= 6;
  const hasUpper = /[A-Z]/.test(pass);
  const hasLower = /[a-z]/.test(pass);
  const hasNumber = /\d/.test(pass);
  const hasSpecial = /[^a-zA-Z0-9]/.test(pass);

  if (!hasMinLength || !hasUpper || !hasLower || !hasNumber || !hasSpecial) {
    return { label: 'Weak', color: 'bg-rose-500' };
  }
  return { label: 'Strong', color: 'bg-emerald-500' };
};

export const PasswordStrengthMeter: React.FC<PasswordStrengthMeterProps> = ({ password }) => {
  const strength = evaluateStrength(password);

  return (
    <div className="space-y-1.5 px-1">
      <div className="flex items-center justify-between text-xs text-neutral-500">
        <span>Password Strength:</span>
        <span
          className={`font-bold ${
            strength.label === 'Strong'
              ? 'text-emerald-600'
              : strength.label === 'Weak'
              ? 'text-rose-600'
              : 'text-neutral-600'
          }`}
        >
          {strength.label}
        </span>
      </div>
      <div className="flex gap-1 h-1.5 w-full bg-neutral-100 rounded-full overflow-hidden">
        <div
          className={`h-full transition-all duration-300 rounded-full ${
            strength.label === 'Strong'
              ? 'w-full bg-emerald-500'
              : strength.label === 'Weak'
              ? 'w-1/3 bg-rose-500'
              : 'w-0 bg-neutral-200'
          }`}
        />
      </div>
      {strength.label === 'Weak' && (
        <p className="text-rose-500 text-[11px] leading-tight">
          Password is too weak. It must be at least 6 characters and contain uppercase, lowercase, numbers, and symbols.
        </p>
      )}
    </div>
  );
};