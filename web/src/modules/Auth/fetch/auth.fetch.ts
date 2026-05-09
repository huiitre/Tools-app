import { clientV3 } from '@/services/axiosInstance';
import {
  useFetchLoginType,
  useFetchLoginWithGoogleType,
  PasswordResetPayload,
  PasswordResetRequestPayload,
  useFetchRegisterType,
} from '../types/auth.types';

export const useFetchLogin = async (credentials: useFetchLoginType) => {
  return await clientV3.post('/auth/login', { ...credentials });
};

export const useFetchLoginWithGoogle = async (
  credentials: useFetchLoginWithGoogleType,
) => {
  return await clientV3.post('/auth/google', { ...credentials });
};

export const useFetchLogout = async () => {
  return await clientV3.post('/auth/logout');
};

export const useFetchMe = async () => {
  return await clientV3.get('/user/me');
};

export const useFetchPasswordReset = async (payload: PasswordResetPayload) => {
  return await clientV3.post('/auth/password/reset', payload);
};

export const useFetchPasswordResetRequest = async (
  payload: PasswordResetRequestPayload,
) => {
  return await clientV3.post('/auth/password/reset-request', payload);
};

export const useFetchRegister = async (credentials: useFetchRegisterType) => {
  return await clientV3.post('/auth/register', credentials);
};

export const useFetchVerifyEmail = async (token: string) => {
  return await clientV3.post(`/auth/verify-email?token=${token}`);
};

export const useFetchSetPassword = async (password: string) => {
  return await clientV3.patch('/user/password', { password });
};