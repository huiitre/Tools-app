// src/utils/passwordValidation.ts
export type PasswordValidationResult = {
  valid: boolean
  error?: string
}

export function validatePassword(
  password: string,
  confirmPassword?: string
): PasswordValidationResult {

  if (password.length < 8) {
    throw new PasswordValidationError('Le mot de passe doit contenir au moins 8 caractères')
  }

  if (!/[A-Za-z]/.test(password)) {
    throw new PasswordValidationError('Le mot de passe doit contenir au moins une lettre')
  }

  if (!/[0-9]/.test(password)) {
    throw new PasswordValidationError('Le mot de passe doit contenir au moins un chiffre')
  }

  if (confirmPassword !== undefined && password !== confirmPassword) {
    throw new PasswordValidationError('Les mots de passe ne correspondent pas')
  }

  return { valid: true }
}

export class PasswordValidationError extends Error {}