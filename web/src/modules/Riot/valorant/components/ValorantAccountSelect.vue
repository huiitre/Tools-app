<script setup lang="ts">
import { ref, computed, nextTick } from 'vue'
import { useRiotStore, type ValorantAccount } from '@/modules/Riot/riot.store'
import { useValorantAccounts } from '../composables/useValorantAccounts'

const riotStore = useRiotStore()
const { openLinkForm, rename, remove } = useValorantAccounts()

const renaming = ref(false)
const renameInput = ref('')
const renameField = ref<HTMLInputElement | null>(null)

const selectedAccountId = computed<number | null>({
  get: () => riotStore.selectedAccountId,
  set: (id) => riotStore.setSelectedAccountId(id),
})

const selectedAccount = computed(() =>
  riotStore.accounts.find(a => a.id === riotStore.selectedAccountId) ?? null
)

function accountLabel(account: ValorantAccount) {
  if (account.label) return account.label
  if (account.gameName) return `${account.gameName}${account.tagLine ? '#' + account.tagLine : ''}`
  return account.puuid.slice(0, 8)
}

async function startRename() {
  if (!selectedAccount.value) return
  renameInput.value = selectedAccount.value.label ?? accountLabel(selectedAccount.value)
  renaming.value = true
  await nextTick()
  renameField.value?.focus()
  renameField.value?.select()
}

async function confirmRename() {
  if (!selectedAccount.value) return
  const label = renameInput.value.trim()
  renaming.value = false
  if (label) await rename(selectedAccount.value.id, label)
}

function cancelRename() {
  renaming.value = false
}

async function removeSelected() {
  if (!selectedAccount.value) return
  await remove(selectedAccount.value.id)
}
</script>

<template>
  <div class="valorant-account-select">
    <template v-if="renaming">
      <input
        ref="renameField"
        v-model="renameInput"
        class="rename-input"
        placeholder="Nom du compte"
        @keyup.enter="confirmRename"
        @keyup.esc="cancelRename"
        @blur="confirmRename"
      />
    </template>
    <template v-else>
      <select
        v-model.number="selectedAccountId"
        class="account-select__input"
        aria-label="Compte Valorant"
        :disabled="!riotStore.accounts.length"
      >
        <option v-if="!riotStore.accounts.length" :value="null">Aucun compte</option>
        <option v-for="a in riotStore.accounts" :key="a.id" :value="a.id">
          {{ accountLabel(a) }}
        </option>
      </select>

      <button
        v-if="selectedAccount"
        type="button"
        class="icon-btn"
        title="Renommer ce compte"
        aria-label="Renommer ce compte"
        @click="startRename"
      >
        <i class="mdi mdi-pencil-outline" />
      </button>

      <button
        v-if="selectedAccount"
        type="button"
        class="icon-btn"
        title="Délier ce compte"
        aria-label="Délier ce compte"
        @click="removeSelected"
      >
        <i class="mdi mdi-link-off" />
      </button>

      <button
        type="button"
        class="icon-btn"
        title="Ajouter un compte"
        aria-label="Ajouter un compte"
        @click="openLinkForm"
      >
        <i class="mdi mdi-plus" />
      </button>
    </template>
  </div>
</template>

<style lang="scss" scoped>
.valorant-account-select {
  display: flex;
  align-items: center;
  gap: 0.4rem;
}

.account-select__input {
  height: 2.25rem;
  width: auto;
  padding: 0 0.5rem;
  font-size: 0.75rem;
  line-height: 1;
  margin: 0;
  min-width: 140px;
}

.rename-input {
  height: 2.25rem;
  width: 160px;
  padding: 0 0.5rem;
  font-size: 0.75rem;
  margin: 0;
}

.icon-btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;

  width: 2.25rem;
  height: 2.25rem;

  padding: 0;
  margin: 0;

  border-radius: var(--pico-border-radius);
  border: 1px solid transparent;

  background: transparent;
  color: var(--pico-muted-color);

  cursor: pointer;

  transition: color 0.15s ease, background-color 0.15s ease, border-color 0.15s ease;

  &:hover {
    color: var(--pico-primary);
    background-color: var(--pico-card-background-color);
    border-color: var(--pico-muted-border-color);
  }

  i {
    font-size: 1.1rem;
    line-height: 1;
  }
}
</style>
