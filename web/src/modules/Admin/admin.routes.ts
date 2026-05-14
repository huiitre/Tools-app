export const routes = [
  {
    name: 'admin',
    path: '/admin',
    component: () => import('@/modules/Admin/Admin.vue'),
    meta: { requireAuth: true, requireAdmin: true },
    redirect: { name: 'admin-dashboard' },
    children: [
      {
        name: 'admin-dashboard',
        path: 'dashboard',
        component: () => import('@/modules/Admin/dashboard/views/AdminDashboard.vue'),
        meta: { label: 'Dashboard' },
      },
      {
        name: 'admin-users',
        path: 'users',
        component: () => import('@/modules/Admin/users/views/AdminUsers.vue'),
        meta: { label: 'Utilisateurs' },
      },
      {
        name: 'admin-modules',
        path: 'modules',
        component: () => import('@/modules/Admin/modules/views/AdminModules.vue'),
        meta: { label: 'Modules' },
      },
      {
        name: 'admin-feedbacks',
        path: 'feedbacks',
        component: () => import('@/modules/Admin/feedbacks/views/AdminFeedbacks.vue'),
        meta: { label: 'Feedbacks' },
      },
    ],
  },
]
