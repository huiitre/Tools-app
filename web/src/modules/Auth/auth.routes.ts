import { RouteLocationNormalizedLoaded } from 'vue-router';

export const routes = [
  {
    name: 'connexion',
    path: '/login',
    component: () => import('@/modules/Auth/views/Auth.vue'),
    meta: { requireAuth: false },
  },
  {
    name: 'verify-email',
    path: '/auth/verify-email',
    component: () => import('@/modules/Auth/views/VerifyEmail.vue'),
    meta: { requireAuth: false },
  },
  {
    name: 'password-reset-request',
    path: '/forgot-password',
    component: () => import('@/modules/Auth/views/PasswordResetRequest.vue'),
    meta: { requireAuth: false },
  },
  {
    name: 'password-reset',
    path: '/auth/reset-password',
    component: () => import('@/modules/Auth/views/PasswordReset.vue'),
    meta: { requireAuth: false },
    props: (route: RouteLocationNormalizedLoaded) => ({
      token: route.query.token,
    }),
  }
]