import { clientCore } from '@/services/axiosInstance';
import {
  useFetchLoginType,
  PasswordResetPayload,
  PasswordResetRequestPayload,
  useFetchRegisterType,
} from '../types/auth.types';

/*
 * Tout ce qui touche à l'identité est servi par l'API Core (/api/core).
 * Le métier (Dofus, Palworld, Riot…) reste sur clientV3.
 */

export const useFetchLogin = async (credentials: useFetchLoginType) => {
  return await clientCore.post('/auth/login', { ...credentials });
};

export const useFetchGoogleAuthUrl = async (source: 'web' | 'electron') => {
  return await clientCore.get<{ url: string }>(`/auth/google/url?source=${source}`);
};

export const useFetchLogout = async () => {
  return await clientCore.post('/auth/logout');
};

export const useFetchMe = async () => {
  return await clientCore.get('/users/me');
};

export const useFetchPasswordReset = async (payload: PasswordResetPayload) => {
  return await clientCore.post('/auth/password/reset', payload);
};

export const useFetchPasswordResetRequest = async (
  payload: PasswordResetRequestPayload,
) => {
  return await clientCore.post('/auth/password/reset-request', payload);
};

export const useFetchRegister = async (credentials: useFetchRegisterType) => {
  return await clientCore.post('/auth/register', credentials);
};

export const useFetchVerifyEmail = async (token: string) => {
  return await clientCore.post(`/auth/verify-email?token=${token}`);
};

//* Le mot de passe est un moyen d'identification, pas une propriété du profil :
//* il vit sous /auth et non sous /users (côté Java, c'était PATCH /user/password).
export const useFetchSetPassword = async (password: string) => {
  return await clientCore.patch('/auth/password', { password });
};

export const useFetchElectronSession = async () => {
  return await clientCore.post('/auth/electron/session');
};
