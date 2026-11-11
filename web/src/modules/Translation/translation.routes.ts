// Le nom de la route racine doit valoir le code du module (`tools_core.module.code`) : c'est ce
// que teste BurgerNav pour afficher l'entrée de menu.
export const routes = [
  {
    name: 'translation',
    path: '/translation',
    component: () => import('@/modules/Translation/views/TranslatorView.vue'),
    meta: { requireAuth: true, requireModule: 'translation' },
  },
]
