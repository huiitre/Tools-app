<script setup lang="ts">
export type SettingsSection =
  | 'account-profile'
  | 'account-security'
  | 'module-dofus'

defineProps<{ currentSection: SettingsSection }>()

const emit = defineEmits<{ (e: 'select', section: SettingsSection): void }>()
</script>

<template>
  <!-- MOBILE -->
  <nav class="settings-nav-mobile mobile-only">
    <details>
      <summary>Navigation</summary>

      <div class="nav-group">
        <span class="nav-label">Compte</span>
        <ul>
          <li :class="{ active: currentSection === 'account-profile' }" @click="emit('select', 'account-profile')">Profil</li>
          <li :class="{ active: currentSection === 'account-security' }" @click="emit('select', 'account-security')">Sécurité</li>
        </ul>
      </div>

      <div class="nav-group">
        <span class="nav-label">Modules</span>
        <ul>
          <li :class="{ active: currentSection === 'module-dofus' }" @click="emit('select', 'module-dofus')">Dofus</li>
        </ul>
      </div>
    </details>
  </nav>

  <!-- DESKTOP -->
  <aside class="settings-sidebar desktop-only">
    <nav>
      <div class="nav-section">
        <span class="nav-label">Compte</span>
        <ul>
          <li :class="{ active: currentSection === 'account-profile' }" @click="emit('select', 'account-profile')">Profil</li>
          <li :class="{ active: currentSection === 'account-security' }" @click="emit('select', 'account-security')">Sécurité</li>
        </ul>
      </div>

      <div class="nav-section">
        <span class="nav-label">Modules</span>
        <ul>
          <li :class="{ active: currentSection === 'module-dofus' }" @click="emit('select', 'module-dofus')">Dofus</li>
        </ul>
      </div>
    </nav>
  </aside>
</template>

<style lang="scss" scoped>
$sidebar-width: 220px;
$label-size: 0.72rem;
$item-size: 0.875rem;

/* ── Desktop sidebar ───────────────────────────────────────────── */
.settings-sidebar {
  width: $sidebar-width;
  min-height: 100%;
  border-right: 1px solid var(--pico-card-border-color);
  padding: 1.5rem 0;
  display: flex;
  flex-direction: column;
}

nav {
  display: flex;
  flex-direction: column;
  justify-content: flex-start;
  gap: 0;
  overflow: visible;
}

.nav-section {
  padding: 0 1rem 1rem;
}

.nav-label {
  display: block;
  font-size: $label-size;
  font-weight: 600;
  letter-spacing: 0.07em;
  text-transform: uppercase;
  color: var(--pico-muted-color);
  padding: 0.75rem 0.5rem 0.4rem;
}

ul {
  list-style: none;
  padding: 0;
  margin: 0;
}

li {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.45rem 0.75rem;
  border-radius: 6px;
  font-size: $item-size;
  cursor: pointer;
  color: var(--pico-color);
  transition: background 0.15s ease, color 0.15s ease;

  &:hover {
    background: var(--pico-card-background-color);
  }

  &.active {
    background: color-mix(in srgb, var(--pico-primary) 12%, transparent);
    color: var(--pico-primary);
    font-weight: 600;
  }
}

/* ── Mobile nav ────────────────────────────────────────────────── */
.settings-nav-mobile {
  padding: 0.75rem 1rem;
  border-bottom: 1px solid var(--pico-card-border-color);

  details summary {
    font-size: $item-size;
    font-weight: 600;
    cursor: pointer;
    list-style: none;
    display: flex;
    align-items: center;
    justify-content: space-between;
  }
}

.nav-group {
  padding: 0.5rem 0;

  &:not(:last-child) {
    border-bottom: 1px solid var(--pico-card-border-color);
  }

  ul {
    display: flex;
    flex-direction: column;
  }
}

/* ── Responsive ────────────────────────────────────────────────── */
.mobile-only { display: none; }
.desktop-only { display: flex; }

@media (max-width: 768px) {
  .desktop-only { display: none; }
  .mobile-only { display: block; }
}
</style>
