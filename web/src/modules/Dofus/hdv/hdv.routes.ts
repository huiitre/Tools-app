import { RouteRecordRaw } from 'vue-router';

export const hdvRoutes: RouteRecordRaw[] = [
  {
    path: '/dofus/hdv',
    name: 'dofus-hdv',
    component: () => import('./views/HdvSniffer.vue'),
    meta: {
      label: 'HDV Sniffer'
    },
  }
];