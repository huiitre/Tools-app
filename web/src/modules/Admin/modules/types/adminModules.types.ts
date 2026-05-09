export type AdminModule = {
  id: number
  name: string
  code: string
  description: string
  active: boolean
}

export type ModuleUser = {
  userId: number
  name: string
  email: string
  roleId: number
  roleCode: string
}

export type CreateModulePayload = {
  name: string
  code: string
  description: string
}

export type UpdateModulePayload = {
  name?: string
  code?: string
  description?: string
  active?: boolean
}
