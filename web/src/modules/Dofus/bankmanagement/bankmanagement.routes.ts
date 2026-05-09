import { RouteRecordRaw } from 'vue-router';

export const bankRoutes: RouteRecordRaw[] = [
  {
    path: '/dofus/bankmanagement',
    name: 'dofus-bankmanagement',
    component: () => import('./views/BankManagement.vue'),
    meta: {
      label: 'Bank Management'
    },
  }
];