import { temtemdexRoutes } from '@/modules/Temtem/temtemdex/temtemdex.routes'
import { temtemTeamsRoutes } from '@/modules/Temtem/teams/teams.routes'

// Le nom de la route racine doit valoir le code du module (`tools_core.module.code`) : c'est ce
// que teste BurgerNav pour décider d'afficher l'entrée de menu.
export const routes = [
  {
    name: 'temtem',
    path: '/temtem',
    component: () => import('@/modules/Temtem/Temtem.vue'),
    meta: { requireAuth: true },
    redirect: { name: 'temtem_temtemdex' },
    children: [
      ...temtemdexRoutes,
      ...temtemTeamsRoutes,
    ],
  },
]
