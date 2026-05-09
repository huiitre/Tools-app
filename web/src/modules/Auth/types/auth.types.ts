export type useFetchLoginType = {
  email: string;
  password: string;
}

export type useFetchLoginWithGoogleType = {
  idToken: string
}

export type PasswordResetPayload = {
  password: string
  token: string
}

export type PasswordResetRequestPayload = {
  email: string
}

export type useFetchRegisterType = {
  name: string
  email: string;
  password: string;
}

export enum RoleCode {
  READ_ONLY = 'READ_ONLY',
  USER = 'USER',
  MODERATOR = 'MODERATOR',
  ADMIN = 'ADMIN',
  TECH = 'TECH',
  OWNER = 'OWNER'
}