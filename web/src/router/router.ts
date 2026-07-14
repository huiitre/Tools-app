import { createRouter, createWebHashHistory, createWebHistory } from 'vue-router';

// components
import Home from '@/modules/Home/Home.vue';

import PrivacyPolicy from '@/modules/Legal/PrivacyPolicy.vue';
import TermsOfService from '@/modules/Legal/TermsOfService.vue';

import NotFound from '@/components/NotFound.vue';
import { useAuthStore } from '@/modules/Auth/auth.store';
import { useFetchMe } from '@/modules/Auth/fetch/auth.fetch';
import { clientInit } from '@/services/axiosInstance';

import { useUIStore } from '@/stores/ui.store';

import { routes as authRoutes } from '@/modules/Auth/auth.routes';
import { routes as dofusRoutes } from '@/modules/Dofus/dofus.routes';
import { routes as riotRoutes } from '@/modules/Riot/riot.routes';
import { routes as adminRoutes } from '@/modules/Admin/admin.routes';
import { routes as eliteDangerousRoutes } from '@/modules/EliteDangerous/elitedangerous.routes';
import { routes as palworldRoutes } from '@/modules/Palworld/palworld.routes';

const isElectron = navigator.userAgent.includes('Electron')

//? meta: { desktopOnly: true } pour limiter la taille d'écran sur une route
export const routes = [
  ...authRoutes,
  ...dofusRoutes,
  ...riotRoutes,
  ...adminRoutes,
  ...eliteDangerousRoutes,
  ...palworldRoutes,
  {
    name: 'home',
    path: '/',
    component: Home,
    meta: { requireAuth: true },
  },
  {
    name: 'DofusSwitcher',
    path: '/dofus-switcher',
    component: () => import('@/modules/Dofus/switcher/views/SwitcherView.vue'),
    meta: { requireAuth: false, standalone: true },
  },
  {
    name: 'settings',
    path: '/settings',
    component: () => import('@/modules/Settings/Settings.vue'),
    meta: { requireAuth: true },
  },
  {
    name: 'Downloads',
    path: '/downloads',
    component: () => import('@/modules/Downloads/Downloads.vue'),
    meta: { requireAuth: false },
  },
  {
    name: 'PrivacyPolicy',
    path: '/privacy-policy',
    component: PrivacyPolicy,
    meta: { requireAuth: false },
  },
  {
    name: 'TermsOfService',
    path: '/terms-of-service',
    component: TermsOfService,
    meta: { requireAuth: false },
  },
  {
    name: 'NotFound',
    path: '/:pathMatch(.*)*',
    component: NotFound,
    meta: { requireAuth: false },
  },
];

const router = createRouter({
  history: isElectron ? createWebHashHistory() : createWebHistory(import.meta.env.BASE_URL),
  routes,
});

router.beforeEach(async (to) => {
  const ui = useUIStore();
  const auth = useAuthStore();

  const requireAuth = to.meta.requireAuth === true;
  const isLogin = to.path === '/login';

  //* Initialisation auth (UNE SEULE FOIS)
  if (!auth.authInitialized) {
    try {
      ui.setLoading(true);

      const { data } = await clientInit.post('/auth/refresh');
      auth.setToken(data.accessToken);

      const me = await useFetchMe();
      auth.setUser(me.data);
    } catch {
      auth.logout();
    } finally {
      ui.setLoading(false);
      auth.setAuthInitialized();
    }
  }

  //* Route protégée sans utilisateur
  if (requireAuth && !auth.user) {
    return '/login';
  }

  //* Route admin sans rang suffisant
  if (to.meta.requireAdmin && !auth.isAdmin) {
    return '/';
  }

  //* Déjà connecté → pas d’accès à /login
  if (auth.user && isLogin) {
    return '/';
  }

  return true;
});

export default router;
