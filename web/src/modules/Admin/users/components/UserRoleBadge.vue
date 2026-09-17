<script setup lang="ts">
import { computed } from 'vue'
import type { AdminRole } from '../types/adminUsers.types'
import { RoleCode } from '@/modules/Auth/types/auth.types'

const props = defineProps<{ role: AdminRole | null }>()

const ROLE_MODIFIERS: Record<RoleCode, string> = {
  [RoleCode.READ_ONLY]: 'read-only',
  [RoleCode.USER]: 'user',
  [RoleCode.MODERATOR]: 'moderator',
  [RoleCode.TECH]: 'tech',
  [RoleCode.ADMIN]: 'admin',
  [RoleCode.OWNER]: 'owner',
}

const modifier = computed(() => props.role ? ROLE_MODIFIERS[props.role.code] : null)
</script>

<template>
  <span class="role-badge" :class="modifier ? `role-badge--${modifier}` : ''">
    {{ role?.name ?? '—' }}
  </span>
</template>

<style scoped lang="scss">
.role-badge {
  display: inline-block;
  padding: 0.15rem 0.5rem;
  border-radius: 999px;
  background: color-mix(in srgb, var(--pico-muted-color) 12%, transparent);
  color: var(--pico-muted-color);
  font-size: 0.75rem;
  font-weight: 600;

  &--owner, &--tech {
    background: color-mix(in srgb, #f59e0b 12%, transparent);
    color: #d97706;
  }

  &--admin {
    background: color-mix(in srgb, var(--pico-primary) 12%, transparent);
    color: var(--pico-primary);
  }

  &--moderator {
    background: color-mix(in srgb, #8b5cf6 12%, transparent);
    color: #7c3aed;
  }

  &--user {
    background: color-mix(in srgb, #22c55e 12%, transparent);
    color: #16a34a;
  }
}
</style>
