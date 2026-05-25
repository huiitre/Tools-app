export const codenameRoutes = [
  {
    name: 'codename',
    path: '/codename',
    component: () => import('@/modules/Codename/Codename.vue'),
    meta: { requireAuth: true },
    redirect: { name: 'codename-history' },
    children: [
      {
        name: 'codename-admin',
        path: 'admin',
        component: () => import('@/modules/Codename/admin/views/CodenameAdmin.vue'),
        meta: { label: 'Administration', requireModerator: true },
      },
      {
        name: 'codename-history',
        path: 'history',
        component: () => import('@/modules/Codename/history/views/CodenameHistory.vue'),
        meta: { label: 'Historique' },
      },
      {
        name: 'codename-propose',
        path: 'propose',
        component: () => import('@/modules/Codename/propose/views/CodenamePropose.vue'),
        meta: { label: 'Proposer un mot' },
      },
      {
        name: 'codename-game',
        path: 'game/:sessionId',
        component: () => import('@/modules/Codename/game/views/CodenameGame.vue'),
        meta: { hideNav: true, requireAuth: false },
      },
    ],
  },
]