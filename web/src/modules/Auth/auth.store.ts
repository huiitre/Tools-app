import { resetSessionStores } from '@/stores/reset'
import { defineStore } from 'pinia'
import { RoleCode, hasAtLeast } from '@/modules/Auth/types/auth.types'

/* ======================
   TYPES MÉTIER
====================== */

export type Role = {
  id: string
  code: string
  name: string
  description: string
  active: boolean
}

// Un utilisateur ne détient qu'un rôle par module : (user_id, module_id) est la clé primaire
// de tools_core.user_module_role.
export type ModuleType = {
  id: string
  code: string
  name: string
  description: string
  active: boolean
  role: Role
}

type User = {
  id: string
  email: string
  name: string
  userType: string
  active: boolean
  avatarUrl: string
  // Rôle global, nul si l'utilisateur n'en a aucun. Au plus un : (user_id) est la clé
  // primaire de tools_core.user_role.
  role: Role | null
  modules: ModuleType[]
}

type AuthState = {
  user: User | null
  accessToken: string | null
  authInitialized: boolean
}

/* ======================
  STORE
====================== */

export const useAuthStore = defineStore('auth', {
  state: (): AuthState => ({
    user: null,
    accessToken: null,
    authInitialized: false
  }),

  getters: {
    isAuthenticated: (s) => !!s.user,

    isAdmin: (s): boolean => {
      const role = s.user?.role
      if (!role?.active) return false
      const adminLevel: string[] = [RoleCode.ADMIN, RoleCode.TECH, RoleCode.OWNER]
      return adminLevel.includes(role.code)
    },

    // Le rôle global ne participe pas : un administrateur du site absent d'un module n'y
    // entre pas, et présent il y vaut ce que son rôle de module dit. Même règle que
    // UseCaseAuthorizer côté API.
    hasModuleAccess: (s) => (moduleCode: string, minRole: RoleCode) => {
      if (!s.user) return false

      const module = s.user.modules.find(m => m.code === moduleCode.toLowerCase() && m.active)
      if (!module?.role.active) return false

      return hasAtLeast(module.role.code, minRole)
    }
  },

  actions: {

    setAuthInitialized() {
      this.authInitialized = true
    },

    setToken(token: string) {
      this.accessToken = token
    },

    setUser(user: User) {
      this.user = user
    },

    logout() {
      this.user = null
      this.accessToken = null

      resetSessionStores()
    }
  }
})
