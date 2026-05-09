export const valorantRoutes = [
  {
    name: 'riot-valorant-shop',
    path: 'valorant-daily-shop',
    component: () => import('@/modules/Riot/valorant/views/ValorantDailyShop.vue'),
    meta: {
      label: 'Valorant Shop',
    },
  },
  {
    name: 'riot-valorant-skins',
    path: 'valorant-skins',
    component: () => import('@/modules/Riot/valorant/views/ValorantSkinCatalog.vue'),
    meta: {
      label: 'Valorant Skins',
    },
  },
]
