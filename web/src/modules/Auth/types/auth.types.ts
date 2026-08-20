export type useFetchLoginType = {
  email: string;
  password: string;
}

export type PasswordResetPayload = {
  password: string
  token: string
}

export type PasswordResetRequestPayload = {
  email: string
}

export type useFetchRegisterType = {
  name: string
  email: string;
  password: string;
}

// Rôles globaux de tools_core.role, déclarés du moins au plus permissif.
//
// L'ordre est celui de l'énumération RoleCode de l'API C#
// (api/Modules/Security/Domain/RoleCode.cs) et doit le rester : TECH est **sous** ADMIN. Les
// deux étaient inversés ici, si bien qu'un TECH satisfaisait une exigence ADMIN côté front
// pendant que l'API la refusait.
export enum RoleCode {
  READ_ONLY = 'READ_ONLY',
  USER = 'USER',
  MODERATOR = 'MODERATOR',
  TECH = 'TECH',
  ADMIN = 'ADMIN',
  OWNER = 'OWNER'
}

// Source unique de la hiérarchie : toute comparaison de rôles passe par `roleRank` ou
// `hasAtLeast`. Chaque copie locale de ce tableau est une occasion de le réordonner de
// travers sans que rien ne le signale.
export const ROLE_HIERARCHY: readonly RoleCode[] = [
  RoleCode.READ_ONLY,
  RoleCode.USER,
  RoleCode.MODERATOR,
  RoleCode.TECH,
  RoleCode.ADMIN,
  RoleCode.OWNER
]

// Rang d'un rôle, -1 s'il est absent ou inconnu. Un code que le front ne connaît pas ne vaut
// aucun droit, comme côté API.
export function roleRank(code: string | null | undefined): number {
  return code ? ROLE_HIERARCHY.indexOf(code as RoleCode) : -1
}

export function hasAtLeast(actual: string | null | undefined, required: RoleCode): boolean {
  const rank = roleRank(actual)
  return rank !== -1 && rank >= roleRank(required)
}
