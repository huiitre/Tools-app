export const breedingRoutes = [
  {
    name: 'palworld-breeding',
    path: 'breeding',
    component: () => import('@/modules/Palworld/breeding/Breeding.vue'),
    meta: { label: 'Élevage', requireAuth: true },
    redirect: { name: 'palworld-breeding-calculator' },
    children: [
      {
        name: 'palworld-breeding-calculator',
        path: 'calculator',
        component: () => import('@/modules/Palworld/breeding/views/PalworldBreedingCalculatorView.vue'),
        meta: { label: "Calculateur d'élevage" },
      },
      {
        name: 'palworld-breeding-search',
        path: 'search',
        component: () => import('@/modules/Palworld/breeding/views/PalworldBreedingSearchView.vue'),
        meta: { label: 'Recherche de combinaisons' },
      },
      {
        name: 'palworld-breeding-path',
        path: 'path',
        component: () => import('@/modules/Palworld/breeding/views/PalworldBreedingPathView.vue'),
        meta: { label: 'Path finder' },
      },
    ],
  },
]
