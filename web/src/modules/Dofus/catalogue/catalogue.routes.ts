export const routes = [
  {
    name: 'dofus-catalogue',
    path: 'catalogue',
    component: () => import('@/modules/Dofus/catalogue/views/Catalogue.vue'),
    meta: {
      label: 'Catalogue',
    },
  }
]