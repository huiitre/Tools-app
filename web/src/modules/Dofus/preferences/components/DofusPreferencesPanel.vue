<script setup lang="ts">
import { DOFUS_PREFERENCES } from '@/modules/Dofus/preferences/data/dofusPreferences'
import { useDofusConfigStore } from '@/modules/Dofus/preferences/preferences.store'
import { PriceDisplayMode } from '@/modules/Dofus/preferences/types/priceDisplayMode.enum'

const dofusConfig = useDofusConfigStore()

const getValue = (key: string) => {
  switch (key) {
    case 'priceDisplayMode':
      return dofusConfig.priceDisplayMode
    case 'showItemContextOnHover':
      return dofusConfig.showItemContextOnHover
    default:
      return null
  }
}
const getBooleanValue = (key: string): boolean => {
  switch (key) {
    case 'showItemContextOnHover':
      return dofusConfig.showItemContextOnHover
    default:
      return false
  }
}

const setValue = (key: string, value: unknown) => {
  switch (key) {
    case 'priceDisplayMode':
      dofusConfig.setPriceDisplayMode(value as PriceDisplayMode)
      break
    case 'showItemContextOnHover':
      dofusConfig.setShowItemContextOnHover(Boolean(value))
      break
  }
}

</script>

<template>
  <div class="dofus-preferences-panel">
    <h3 class="panel-title">Préférences</h3>

    <div
      v-for="pref in DOFUS_PREFERENCES"
      :key="pref.key"
      class="preference-item"
    >
      <label class="preference-label">
        {{ pref.label }}
      </label>

      <p
        v-if="pref.description"
        class="preference-description"
      >
        {{ pref.description }}
      </p>

      <!-- SELECT -->
      <select
        v-if="pref.type === 'select'"
        class="preference-select"
        :value="getValue(pref.key)"
        @change="setValue(pref.key, ($event.target as HTMLSelectElement).value)"
      >
        <option
          v-for="option in pref.options"
          :key="option.value"
          :value="option.value"
        >
          {{ option.label }}
        </option>
      </select>

      <p
        v-if="pref.type === 'select'"
        class="preference-help"
      >
        {{
          pref.options.find(o => o.value === getValue(pref.key))?.help
        }}
      </p>

      <!-- SWITCH -->
      <label
        v-if="pref.type === 'switch'"
        class="preference-switch"
      >
        <input
          type="checkbox"
          role="switch"
          :checked="getBooleanValue(pref.key)"
          @change="setValue(pref.key, ($event.target as HTMLInputElement).checked)"
        />
      </label>
    </div>
  </div>
</template>

<style lang="scss" scoped>
.dofus-preferences-panel {
  display: flex;
  flex-direction: column;
  gap: 0.9rem;
  width: 400px;
  font-size: 0.75rem;
}

.panel-title {
  margin: 0 0 0.25rem 0;
  font-size: 1.2rem;
  font-weight: 600;
  color: var(--pico-color);
}

.preference-item {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.preference-label {
  font-weight: 500;
  color: var(--pico-primary);
}

.preference-description {
  font-size: 0.7rem;
  color: var(--pico-muted-color);
  margin-bottom: 0.2rem;
}

.preference-select {
  font-size: 0.75rem;
  margin: 0;
}

/* Switch Pico – aligné visuellement avec le select */
.preference-switch {
  display: flex;
  align-items: center;
  margin-top: 0.1rem;
}

.preference-switch input[type='checkbox'] {
  margin: 0;
}

.preference-help {
  font-size: 0.65rem;
  color: var(--pico-muted-color);
}
</style>
