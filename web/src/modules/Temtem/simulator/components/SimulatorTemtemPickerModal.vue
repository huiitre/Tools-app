<script setup lang="ts">
import { computed, ref } from 'vue'
import { useTemtemdexStore } from '@/modules/Temtem/temtemdex/temtemdex.store'
import { dexNumber, typesOf } from '@/modules/Temtem/shared/temtem.helpers'
import type { TemtemSummary } from '@/modules/Temtem/shared/types/temtem.types'

const emit = defineEmits<{
  (e: 'close'): void
  (e: 'pick', temtem: TemtemSummary): void
}>()

const props = defineProps<{
  recentIds: number[]
}>()

const dexStore = useTemtemdexStore()
const query = ref('')

const results = computed(() => {
  const needle = query.value.trim().toLocaleLowerCase()
  const temtem = needle
    ? dexStore.temtem.filter(item => item.name.toLocaleLowerCase().includes(needle))
    : dexStore.temtem

  return temtem.slice(0, 60)
})

const recentTemtem = computed(() => props.recentIds
  .map(id => dexStore.temtem.find(temtem => temtem.id === id))
  .filter((temtem): temtem is TemtemSummary => temtem !== undefined))
</script>

<template>
  <div class="overlay" @click.self="emit('close')">
    <section class="modal" role="dialog" aria-modal="true" aria-label="Choisir un Temtem adverse">
      <header>
        <h3>Choisir un adversaire</h3>
      </header>

      <section v-if="recentTemtem.length" class="recents">
        <p>Récemment sélectionnés</p>
        <div class="recent-grid">
          <button v-for="temtem in recentTemtem" :key="temtem.id" type="button" class="recent" @click="emit('pick', temtem)">
            <img v-if="temtem.imageUrl" :src="temtem.imageUrl" :alt="temtem.name">
            <span>{{ temtem.name }}</span>
            <span class="types"><img v-for="type in typesOf(temtem)" :key="type.id" :src="type.imageUrl ?? ''" :alt="type.name"></span>
          </button>
        </div>
      </section>

      <div class="search">
        <i class="mdi mdi-magnify" />
        <input v-model="query" type="search" placeholder="Rechercher un Temtem…" autofocus>
      </div>

      <div class="results">
        <button v-for="temtem in results" :key="temtem.id" type="button" class="result" @click="emit('pick', temtem)">
          <img v-if="temtem.imageUrl" :src="temtem.imageUrl" :alt="temtem.name" loading="lazy">
          <span class="name">{{ temtem.name }}</span>
          <span class="types">
            <img v-for="type in typesOf(temtem)" :key="type.id" :src="type.imageUrl ?? ''" :alt="type.name" :title="type.name">
          </span>
          <small>{{ dexNumber(temtem) }}</small>
        </button>
        <p v-if="!results.length">Aucun Temtem ne correspond.</p>
      </div>

      <footer><button type="button" class="secondary" @click="emit('close')">Fermer</button></footer>
    </section>
  </div>
</template>

<style scoped lang="scss">
.overlay { position: fixed; inset: 0; z-index: 1000; display: grid; place-items: center; padding: 1rem; background: rgb(0 0 0 / 45%); }
.modal { width: min(440px, 100%); max-height: min(80vh, 650px); display: flex; flex-direction: column; margin: 0; padding: 0; overflow: hidden; border: 1px solid var(--pico-muted-border-color); border-radius: var(--pico-border-radius); background: var(--pico-card-background-color); }
header, footer { padding: .75rem 1rem; border-color: var(--pico-muted-border-color); }
header { border-bottom: 1px solid var(--pico-muted-border-color); }
h3 { margin: 0; font-size: 1rem; }
.recents { padding: .55rem 1rem .65rem; border-bottom: 1px solid var(--pico-muted-border-color); }
.recents > p { margin: 0 0 .45rem; color: var(--pico-muted-color); font-size: .7rem; font-weight: 700; text-transform: uppercase; }
.recent-grid { display: grid; grid-template-columns: repeat(5, minmax(0, 1fr)); gap: .35rem; }
.recent { display: grid; justify-items: center; gap: .2rem; min-width: 0; margin: 0; padding: .35rem .2rem; border: 1px solid var(--pico-card-border-color); border-radius: 6px; background: var(--pico-card-sectioning-background-color); color: var(--pico-color); }
.recent:hover, .recent:hover span { border-color: var(--pico-primary); color: var(--pico-primary) !important; }
.recent > img { width: 32px; height: 32px; border-radius: 4px; object-fit: cover; }
.recent > span:not(.types) { width: 100%; overflow: hidden; font-size: .68rem; text-align: center; text-overflow: ellipsis; white-space: nowrap; }
.search { display: flex; align-items: center; gap: .45rem; padding: .55rem 1rem; border-bottom: 1px solid var(--pico-muted-border-color); color: var(--pico-muted-color); }
.search input { height: 2rem; margin: 0; padding: 0; border: 0; background: transparent; box-shadow: none; font-size: .82rem; }
.results { flex: 1; overflow-y: auto; padding: .4rem; }
.result { display: flex; align-items: center; width: 100%; gap: .55rem; margin: 0; padding: .35rem .5rem; border: 0; border-radius: var(--pico-border-radius); background: transparent; color: var(--pico-color); text-align: left; }
.result:hover { background: var(--pico-card-sectioning-background-color); color: var(--pico-primary) !important; }
.result:hover .name { color: var(--pico-primary) !important; }
.result > img { width: 34px; height: 34px; border-radius: 4px; }
.name { flex: 1; font-size: .84rem; }.types { display: flex; gap: .2rem; }.types img { width: 17px; height: 17px; border-radius: 3px; }.result small { color: var(--pico-muted-color); }
.results p { margin: 1rem; text-align: center; color: var(--pico-muted-color); } footer { border-top: 1px solid var(--pico-muted-border-color); } footer button { width: 100%; margin: 0; }
</style>
