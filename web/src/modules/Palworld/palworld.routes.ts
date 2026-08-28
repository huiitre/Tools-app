import { tierlistRoutes } from '@/modules/Palworld/tierlist/tierlist.routes'
import { paldexRoutes } from '@/modules/Palworld/paldex/paldex.routes'
import { breedingRoutes } from '@/modules/Palworld/breeding/breeding.routes'
import { inventoryRoutes } from '@/modules/Palworld/inventory/inventory.routes'
import { catalogRoutes } from '@/modules/Palworld/catalog/catalog.routes'
import { shopRoutes } from '@/modules/Palworld/shop/shop.routes'

export const routes = [
  {
    name: 'palworld',
    path: '/palworld',
    component: () => import('@/modules/Palworld/Palworld.vue'),
    meta: { requireAuth: true },
    redirect: { name: 'palworld-tierlist' },
    children: [
      ...tierlistRoutes,
      ...paldexRoutes,
      ...inventoryRoutes,
      ...breedingRoutes,
      ...catalogRoutes,
      ...shopRoutes,
    ],
  },
]
