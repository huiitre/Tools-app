import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type { AdminModule, ModuleUser } from '../types/adminModules.types'
import type { AdminRole, AdminUser } from '../../users/types/adminUsers.types'

export const useAdminModulesStore = defineStore('adminModules', () => {
  const modules = ref<AdminModule[]>([])
  const roles = ref<AdminRole[]>([])
  const selectedModuleId = ref<number | null>(null)
  const moduleUsers = ref<ModuleUser[]>([])
  const allUsers = ref<AdminUser[]>([])
  const loadingModules = ref(false)
  const loadingUsers = ref(false)

  const selectedModule = computed(() =>
    modules.value.find(m => m.id === selectedModuleId.value) ?? null
  )

  // userId est un number côté API, id est string côté AdminUser — on compare en string
  const memberIds = computed(() => new Set(moduleUsers.value.map(u => String(u.userId))))

  const availableUsers = computed(() =>
    allUsers.value.filter(u => !memberIds.value.has(String(u.id)))
  )

  function selectModule(id: number) {
    selectedModuleId.value = id
    moduleUsers.value = []
  }

  function addModuleLocally(m: AdminModule) {
    modules.value.push(m)
  }

  function updateModuleLocally(updated: AdminModule) {
    const idx = modules.value.findIndex(m => m.id === updated.id)
    if (idx !== -1) modules.value[idx] = updated
  }

  function addMemberLocally(user: AdminUser, roleId: number, roleCode: string) {
    moduleUsers.value.push({
      userId: Number(user.id),
      name: user.name,
      email: user.email,
      roleId,
      roleCode,
    })
  }

  function removeMemberLocally(userId: string | number) {
    moduleUsers.value = moduleUsers.value.filter(u => String(u.userId) !== String(userId))
  }

  function updateMemberRoleLocally(userId: string | number, roleCode: string) {
    const u = moduleUsers.value.find(u => String(u.userId) === String(userId))
    if (u) u.roleCode = roleCode
  }

  return {
    modules, roles, selectedModuleId, moduleUsers, allUsers,
    loadingModules, loadingUsers,
    selectedModule, availableUsers, memberIds,
    selectModule, addModuleLocally, updateModuleLocally,
    addMemberLocally, removeMemberLocally, updateMemberRoleLocally,
  }
})
