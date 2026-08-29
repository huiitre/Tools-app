<script setup lang="ts">
import { ref } from 'vue'
import { useRiotStore, type RiotRegion } from '@/modules/Riot/riot.store'

const props = defineProps<{
  error: string | null
  regions: { value: RiotRegion; label: string }[]
}>()

const emit = defineEmits<{
  submit: [{ token: string; region: RiotRegion }]
}>()

const riotStore = useRiotStore()

const tokenInput = ref('')
const showToken = ref(false)
const selectedRegion = ref<RiotRegion>(riotStore.region)

function onSubmit() {
  const token = tokenInput.value.trim()
  if (!token) return
  emit('submit', { token, region: selectedRegion.value })
}
</script>

<template>
  <div class="auth-card">
    <div class="auth-header">
      <i class="mdi mdi-crosshairs auth-icon" />
      <div>
        <h3 class="auth-title">Lier un compte Valorant</h3>
        <p class="auth-subtitle">Consultez votre boutique sans ouvrir le jeu</p>
      </div>
    </div>

    <div v-if="props.error" class="error-banner" role="alert">
      <i class="mdi mdi-alert-circle-outline" />
      {{ props.error }}
    </div>

    <label>
      Région
      <select v-model="selectedRegion">
        <option v-for="r in props.regions" :key="r.value" :value="r.value">{{ r.label }}</option>
      </select>
    </label>

    <label>
      Refresh Token
      <div class="token-input-wrap">
        <input
          :type="showToken ? 'text' : 'password'"
          v-model="tokenInput"
          placeholder="Collez votre __Secure-refresh_token ici..."
        />
        <button
          type="button"
          class="token-eye"
          aria-label="Afficher/masquer le token"
          @click="showToken = !showToken"
        >
          <i :class="['mdi', showToken ? 'mdi-eye-off' : 'mdi-eye']" />
        </button>
      </div>
    </label>

    <p class="help-note">
      Une fois lié, votre compte sera automatiquement maintenu à jour par le serveur.
    </p>

    <details>
      <summary>Comment récupérer mon token ?</summary>
      <div class="help-content">
        <ol class="help-steps">
          <li>Connectez-vous sur <a href="https://playvalorant.com" target="_blank" rel="noopener">playvalorant.com</a></li>
          <li>Ouvrez les DevTools (F12) → onglet <strong>Application</strong></li>
          <li>Dans le panneau gauche : <strong>Cookies</strong> → <code>https://playvalorant.com</code></li>
          <li>Cherchez <code>__Secure-refresh_token</code> et copiez la colonne <strong>Value</strong></li>
        </ol>
        <p class="help-note" style="background: transparent; border-color: transparent; color: var(--pico-muted-color);">
          Ce cookie est HttpOnly — non lisible via la console JS, utilisez uniquement l'onglet Application.
        </p>
      </div>
    </details>

    <button :disabled="!tokenInput.trim()" @click="onSubmit">
      Lier ce compte
    </button>
  </div>
</template>

<style lang="scss" scoped>
.auth-card {
  width: 100%;
  max-width: 520px;
  background: var(--pico-card-background-color);
  border: 1px solid var(--pico-card-border-color);
  border-radius: 12px;
  padding: 2rem;
  display: flex;
  flex-direction: column;
  gap: 1.2rem;
}

.auth-header {
  display: flex;
  align-items: center;
  gap: 1rem;
}

.auth-icon {
  font-size: 2.25rem;
  color: var(--pico-primary);
  flex-shrink: 0;
}

.auth-title {
  margin: 0;
  font-size: 1.2rem;
  line-height: 1.2;
}

.auth-subtitle {
  margin: 0.2rem 0 0;
  font-size: 0.85rem;
  color: var(--pico-muted-color);
}

.error-banner {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.75rem 1rem;
  border-radius: 8px;
  background: color-mix(in srgb, #e53e3e 10%, transparent);
  border: 1px solid color-mix(in srgb, #e53e3e 25%, transparent);
  color: #e53e3e;
  font-size: 0.875rem;
  line-height: 1.4;
}

label {
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
  margin-bottom: 0;
}

.token-input-wrap {
  position: relative;

  input {
    padding-right: 2.75rem;
    margin: 0;
  }
}

.token-eye {
  position: absolute;
  top: 50%;
  right: 0.6rem;
  transform: translateY(-50%);
  background: transparent;
  border: none;
  padding: 0.25rem;
  margin: 0;
  cursor: pointer;
  color: var(--pico-muted-color);
  font-size: 1.1rem;
  line-height: 1;
  transition: color 0.15s ease;
  width: auto;

  &:hover {
    color: inherit;
    background: transparent;
  }
}

details a {
  color: var(--pico-primary);
  text-decoration: none;
  transition: color 0.15s ease;

  &:hover { text-decoration: underline; }
}

.help-content {
  margin-top: 0.75rem;
  display: flex;
  flex-direction: column;
  gap: 0.65rem;
}

.help-note {
  margin: 0;
  font-size: 0.82rem;
  color: var(--pico-primary);
  padding: 0.5rem 0.75rem;
  border-radius: 6px;
  background: color-mix(in srgb, var(--pico-primary) 8%, transparent);
  border: 1px solid color-mix(in srgb, var(--pico-primary) 20%, transparent);
}

.help-steps {
  margin: 0;
  padding-left: 1.2rem;
  font-size: 0.85rem;
  display: flex;
  flex-direction: column;
  gap: 0.3rem;
  color: var(--pico-muted-color);

  code {
    font-size: 0.8em;
    background: var(--pico-code-background-color);
    padding: 0.1em 0.35em;
    border-radius: 4px;
    color: var(--pico-code-color);
  }
}
</style>
