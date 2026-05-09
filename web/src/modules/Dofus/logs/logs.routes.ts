export const logsRoutes = [
  {
    name: 'dofus-logs',
    path: 'logs',
    component: () => import('@/modules/Dofus/logs/LogsView.vue'),
    meta: {
      label: 'Logs',
    },
  },
]
