import type { RouteLocationNormalized } from 'vue-router'
import { useAuthStore } from '@/modules/Auth/auth.store'
import { RoleCode } from '@/modules/Auth/types/auth.types'

//* Une route peut exiger l'accès à un module fonctionnel, déclaré par `meta.requireModule` avec
//* le code de `tools_core.module.code`.
//*
//* La meta se pose sur la route **parente** du module : vue-router fusionne les meta de tous les
//* records d'une navigation, une déclaration couvre donc tous les enfants, présents et à venir.
//*
//* Le seuil est READ_ONLY, soit « ce module m'est ouvert ». Une action plus exigeante à
//* l'intérieur reste l'affaire de l'API, qui répond 403 : le routeur ouvre la page, il ne
//* prétend pas connaître le droit de chaque bouton.
export function isModuleAllowed(route: RouteLocationNormalized): boolean {
  const moduleCode = route.meta.requireModule as string | undefined

  if (!moduleCode) return true

  return useAuthStore().hasModuleAccess(moduleCode, RoleCode.READ_ONLY)
}
