import { resetSessionStores } from '@/stores/reset'
import { defineStore } from 'pinia'
import { RoleCode, hasAtLeast } from '@/modules/Auth/types/auth.types'
import { useFetchMe } from '@/modules/Auth/fetch/auth.fetch'
import { coreHubConnection } from '@/modules/Core/Realtime/infrastructure/coreHubConnection'

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
  realtimeSyncArmed: boolean
}

/* ======================
  STORE
====================== */

export const useAuthStore = defineStore('auth', {
  state: (): AuthState => ({
    user: null,
    accessToken: null,
    authInitialized: false,
    realtimeSyncArmed: false
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
      this.armRealtimeSync()
    },

    //* Rafraîchit le profil (rôle + modules) sans toucher au token — utile après une action
    //* qui change les droits de l'utilisateur courant sans que sa session ait besoin d'être
    //* renouvelée (contrairement à refreshSession(), qui fait aussi un POST /auth/refresh).
    async refreshUser() {
      const { data } = await useFetchMe()
      this.setUser(data)
    },

    //* Écoute une seule fois pour toute la session : un admin qui modifie le rôle global ou
    //* l'accès module de l'utilisateur courant pendant qu'il est connecté doit lui faire revoir
    //* ses droits sans qu'il ait à recharger la page. N'a rien à voir avec les notifications,
    //* donc pas de dépendance à ce module — juste la connexion hub partagée.
    armRealtimeSync() {
      if (this.realtimeSyncArmed) return
      this.realtimeSyncArmed = true

      const onRightsChanged = () => this.refreshUser()
      coreHubConnection.on('Core.UserGlobalRoleChanged', onRightsChanged)
      coreHubConnection.on('Core.UserModuleRoleChanged', onRightsChanged)
      coreHubConnection.on('Core.UserModuleAccessGranted', onRightsChanged)
      coreHubConnection.on('Core.UserModuleAccessRevoked', onRightsChanged)
      // Ciblé côté API sur les membres du module (FindByModuleIdAsync) : quiconque reçoit cet
      // event en fait déjà partie, pas besoin de le revérifier ici.
      coreHubConnection.on('Core.ModuleUpdated', onRightsChanged)
    },

    logout() {
      this.user = null
      this.accessToken = null

      resetSessionStores()
    }
  }
})
