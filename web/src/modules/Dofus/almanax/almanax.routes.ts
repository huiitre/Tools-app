export const routes = [
  {
    name: 'dofus-almanax',
    path: 'almanax',
    component: () => import('@/modules/Dofus/almanax/views/Almanax.vue'),
    meta: {
      label: 'Almanax',
    },
  }
]