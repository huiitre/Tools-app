<script setup lang="ts">
import { computed, vModelSelect } from 'vue'
import { useDofusStore } from '@/modules/Dofus/dofus.store'

const dofusStore = useDofusStore()

const gameVersions = computed(() => dofusStore.gameVersions)
const selectedGameVersionId = computed<number | null>({
  get: () => dofusStore.currentGameVersionId,
  set: (id) => {
    if (id !== null) {
      dofusStore.setCurrentGameVersion(id)
    }
  }
})

const gameServers = computed(() => dofusStore.gameServers)
const selectedGameServerId = computed<number | null>({
  get: () => dofusStore.currentGameServerId,
  set: (id) => {
    if (id !== null) {
      dofusStore.setCurrentGameServer(id)
    }
  }
})

</script>

<template>
  <div class="dofus-game-select">
    <select
      v-model.number="selectedGameVersionId"
      class="dofus-gameversion-select__input"
      aria-label="Version du jeu"
      :disabled="gameVersions.length === 0"
    >
      <option
        v-for="version in gameVersions"
        :key="version.id"
        :value="version.id"
      >
        {{ version.name }}
      </option>
    </select>
    <select
      v-model.number="selectedGameServerId"
      :disabled="gameServers.length === 0"
      class="dofus-gameserver-select__input"
      aria-label="Serveur de jeu"
    >
      <option
        v-for="server in gameServers"
        :key="server.id"
        :value="server.id"
      >
        {{ server.name }}
      </option>
    </select>
  </div>
</template>

<style scoped lang="scss">
.dofus-game-select {
  display: flex;
  align-items: center;
  padding-left: 0.6rem;
  gap: 1rem;
}

.dofus-gameversion-select__input,
.dofus-gameserver-select__input {
  height: 2rem;
  width: auto;
  padding: 0 0.5rem;

  font-size: 0.75rem;
  line-height: 1;
  margin: 0;

  min-width: 120px;
}
</style>
