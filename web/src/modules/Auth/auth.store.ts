import { resetSessionStores } from '@/stores/reset'
import { defineStore } from 'pinia'
import { RoleCode } from '@/modules/Auth/types/auth.types'

/* ======================
   TYPES MÉTIER
====================== */

type Role = {
  id: string
  code: string
  name: string
  description: string
  active: boolean
}

export type ModuleType = {
  id: string
  code: string
  name: string
  description: string
  active: boolean
  roles: Role[]
}

type User = {
  id: string
  email: string
  name: string
  userType: string
  active: boolean
  avatarUrl: string
  roles: Role[]
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
      if (!s.user) return false
      const adminLevel: string[] = [RoleCode.ADMIN, RoleCode.TECH, RoleCode.OWNER]
      return s.user.roles.some(r => r.active && adminLevel.includes(r.code))
    },

    hasModuleAccess: (s) => (moduleCode: string, minRole: RoleCode) => {

      if (!s.user) return false

      const module = s.user.modules.find(m => m.code === moduleCode.toLowerCase() && m.active)
      if (!module) return false

      const hierarchy = [
        RoleCode.READ_ONLY,
        RoleCode.USER,
        RoleCode.MODERATOR,
        RoleCode.ADMIN,
        RoleCode.TECH,
        RoleCode.OWNER
      ]

      const userMaxRole = module.roles
        .filter(r => r.active)
        .map(r => hierarchy.indexOf(r.code as RoleCode))
        .reduce((max, current) => Math.max(max, current), -1)

      const minIndex = hierarchy.indexOf(minRole)

      return userMaxRole >= minIndex
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
