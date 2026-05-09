<script setup lang="ts">
import { ref } from 'vue'
import SettingsNav, { type SettingsSection } from './SettingsNav.vue'
import AccountProfileSection from './sections/AccountProfileSection.vue'
import AccountSecuritySection from './sections/AccountSecuritySection.vue'
import AutofocusMappingSection from './sections/AutofocusMappingSection.vue'
import { useAuthStore } from '@/modules/Auth/auth.store'

const auth = useAuthStore()
const currentSection = ref<SettingsSection>('account-profile')
</script>

<template>
  <div class="settings-layout">
    <SettingsNav :current-section="currentSection" @select="currentSection = $event" />

    <main class="settings-content">
      <AccountProfileSection
        v-if="currentSection === 'account-profile'"
        :user="auth.user"
      />

      <AccountSecuritySection v-if="currentSection === 'account-security'" />

      <section v-if="currentSection === 'module-dofus'">
        <AutofocusMappingSection />
      </section>
    </main>
  </div>
</template>

<style scoped>
.settings-layout {
  display: flex;
  min-height: 100vh;
}

.settings-content {
  flex: 1;
  padding: 2rem;
  min-width: 0;
}

@media (max-width: 768px) {
  .settings-layout {
    flex-direction: column;
  }
}
</style>
