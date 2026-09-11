<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref } from 'vue'
import { fetchGameServerMods } from '../fetch/gameServers.fetch'
import type { GameServer, GameServerMods } from '../types/gameServers.types'
import { formatBytes } from '@/utils/formatBytes'
import { normalizeSearchText } from '@/utils/searchNormalize'

const props = defineProps<{ server: GameServer }>()
const emit = defineEmits<{ close: [] }>()

const mods = ref<GameServerMods | null>(null)
const loading = ref(true)
const error = ref<string | null>(null)
const search = ref('')

// Une icône d'hébergeur peut disparaître : on retombe alors sur le pictogramme, comme sans icône.
const brokenIcons = ref(new Set<string>())

let previousBodyOverflow = ''

const filteredMods = computed(() => {
  const list = mods.value?.mods ?? []
  const query = normalizeSearchText(search.value.trim())
  if (!query) return list
  return list.filter(mod =>
    normalizeSearchText([mod.name, ...mod.authors].join(' ')).includes(query)
  )
})

const downloadLabel = computed(() =>
  mods.value?.modpackSize ? `Télécharger les mods (${formatBytes(mods.value.modpackSize)})` : 'Télécharger les mods'
)

function iconOf(iconUrl: string | null): string | null {
  return iconUrl && !brokenIcons.value.has(iconUrl) ? iconUrl : null
}

function onIconError(iconUrl: string) {
  brokenIcons.value = new Set(brokenIcons.value).add(iconUrl)
}

function onKeydown(event: KeyboardEvent) {
  if (event.key === 'Escape') emit('close')
}

onMounted(async () => {
  document.addEventListener('keydown', onKeydown)
  previousBodyOverflow = document.body.style.overflow
  document.body.style.overflow = 'hidden'

  try {
    mods.value = await fetchGameServerMods(props.server.slug)
  } catch {
    error.value = 'Impossible de charger les mods du serveur.'
  } finally {
    loading.value = false
  }
})

onUnmounted(() => {
  document.removeEventListener('keydown', onKeydown)
  document.body.style.overflow = previousBodyOverflow
})
</script>

<template>
  <div class="mods-overlay" @click.self="emit('close')">
    <div class="mods-popup">
      <div class="mods-header">
        <img v-if="server.pictureUrl" class="mods-picture" :src="server.pictureUrl" :alt="server.gameName" />
        <div class="mods-identity">
          <span class="mods-title">Mods</span>
          <span class="mods-subtitle">{{ server.gameName }} — {{ server.serverName }}</span>
        </div>
        <i class="mdi mdi-close mods-close" @click="emit('close')" />
      </div>

      <div class="mods-toolbar">
        <input
          v-model="search"
          type="search"
          class="mods-search"
          placeholder="Rechercher un mod ou un auteur"
          :disabled="loading || !mods?.mods.length"
        />
        <span v-if="mods?.mods.length" class="mods-count">
          {{ filteredMods.length }} / {{ mods.mods.length }} mods
        </span>

        <!-- Lien direct vers les assets : le fichier ne transite jamais par l'API. -->
        <a
          v-if="mods?.modpackUrl"
          class="mods-download"
          role="button"
          :href="mods.modpackUrl"
          download
        >
          <i class="mdi mdi-download" aria-hidden="true" />
          {{ downloadLabel }}
        </a>
      </div>

      <div class="mods-body">
        <div v-if="error" class="mods-error">
          <i class="mdi mdi-alert-circle-outline" />
          {{ error }}
        </div>

        <p v-else-if="loading" class="mods-empty">Chargement…</p>

        <p v-else-if="!mods?.mods.length" class="mods-empty">Aucun mod déclaré pour ce serveur.</p>

        <p v-else-if="!filteredMods.length" class="mods-empty">Aucun mod ne correspond à la recherche.</p>

        <ul v-else class="mods-grid">
          <li v-for="mod in filteredMods" :key="mod.name + (mod.fileName ?? '')" class="mod-card">
            <div class="mod-icon">
              <img
                v-if="iconOf(mod.iconUrl)"
                :src="iconOf(mod.iconUrl)!"
                :alt="mod.name"
                loading="lazy"
                @error="onIconError(mod.iconUrl!)"
              />
              <i v-else class="mdi mdi-puzzle-outline" aria-hidden="true" />
            </div>

            <div class="mod-content">
              <a
                v-if="mod.url"
                class="mod-name"
                :href="mod.url"
                target="_blank"
                rel="noopener noreferrer"
                :title="mod.name"
              >{{ mod.name }}</a>
              <span v-else class="mod-name" :title="mod.name">{{ mod.name }}</span>

              <span v-if="mod.version" class="mod-version" :title="mod.version">{{ mod.version }}</span>
              <span v-if="mod.authors.length" class="mod-authors" :title="mod.authors.join(', ')">
                {{ mod.authors.join(', ') }}
              </span>
            </div>
          </li>
        </ul>
      </div>
    </div>
  </div>
</template>

<style lang="scss" scoped>
/* Même cadre que GameServerDashboardModal, en plus étroit : une liste n'a pas besoin de 1300 px. */
.mods-overlay {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.6);
  z-index: 1000;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 1rem;
}

.mods-popup {
  background: var(--pico-background-color);
  border: 1px solid var(--pico-card-border-color);
  border-radius: var(--pico-border-radius);
  box-shadow: var(--pico-card-box-shadow);
  width: 100%;
  max-width: 1000px;
  height: 85vh;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.mods-header {
  display: flex;
  align-items: center;
  gap: 0.85rem;
  padding: 0.85rem 1.25rem;
  border-bottom: 1px solid var(--pico-card-border-color);
  flex-shrink: 0;
}

.mods-picture {
  width: 3.5rem;
  height: 2rem;
  object-fit: cover;
  border-radius: calc(var(--pico-border-radius) / 2);
  flex-shrink: 0;
}

.mods-identity {
  display: flex;
  flex-direction: column;
  min-width: 0;
  margin-right: auto;
}

.mods-title {
  font-weight: 700;
  font-size: 1.05rem;
}

.mods-subtitle {
  font-size: 0.8rem;
  color: var(--pico-muted-color);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.mods-close {
  cursor: pointer;
  color: var(--pico-muted-color);
  font-size: 1.2rem;

  &:hover { color: var(--pico-color); }
}

.mods-toolbar {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 0.75rem;
  padding: 0.75rem 1.25rem;
  border-bottom: 1px solid var(--pico-card-border-color);
  flex-shrink: 0;
}

.mods-search {
  flex: 1;
  min-width: 12rem;
  margin: 0;
  padding: 0.35rem 0.75rem;
  height: auto;
  font-size: 0.85rem;
}

.mods-count {
  font-size: 0.8rem;
  color: var(--pico-muted-color);
  white-space: nowrap;
}

.mods-download {
  margin: 0;
  margin-left: auto;
  padding: 0.4rem 0.9rem;
  display: inline-flex;
  align-items: center;
  gap: 0.4rem;
  font-size: 0.85rem;
  font-weight: 600;
  white-space: nowrap;

  // Fond primaire de Pico : main.scss rétablit la couleur du texte dans tout [role=button] à
  // classe, il faut donc reposer ici celle qui reste lisible sur ce fond, survol compris.
  &,
  &:hover {
    color: var(--pico-primary-inverse);
  }
}

.mods-body {
  flex: 1;
  min-height: 0;
  overflow-y: auto;
  overscroll-behavior: contain;
  padding: 1.25rem;
}

.mods-error {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.75rem 1rem;
  border-radius: 8px;
  background: color-mix(in srgb, #e53e3e 10%, transparent);
  border: 1px solid color-mix(in srgb, #e53e3e 25%, transparent);
  color: #e53e3e;
  font-size: 0.875rem;
}

.mods-empty {
  margin: 0;
  padding: 2rem 0;
  text-align: center;
  font-size: 0.85rem;
  color: var(--pico-muted-color);
}

.mods-grid {
  list-style: none;
  margin: 0;
  padding: 0;
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(17rem, 1fr));
  gap: 0.75rem;
}

.mod-card {
  list-style: none;
  margin: 0;
  display: flex;
  align-items: center;
  gap: 0.75rem;
  padding: 0.6rem 0.75rem;
  background: var(--pico-card-background-color);
  border: 1px solid var(--pico-card-border-color);
  border-radius: var(--pico-border-radius);
  min-width: 0;
}

.mod-icon {
  width: 2.5rem;
  height: 2.5rem;
  flex-shrink: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: calc(var(--pico-border-radius) / 2);
  background: var(--pico-muted-background-color);
  overflow: hidden;

  img {
    width: 100%;
    height: 100%;
    object-fit: cover;
  }

  .mdi {
    font-size: 1.3rem;
    opacity: 0.4;
  }
}

.mod-content {
  display: flex;
  flex-direction: column;
  min-width: 0;

  > * {
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }
}

.mod-name {
  font-size: 0.9rem;
  font-weight: 600;
  color: inherit;
  text-decoration: none;
}

a.mod-name:hover {
  color: var(--pico-primary);
  text-decoration: underline;
}

.mod-version {
  font-size: 0.75rem;
  font-family: var(--pico-font-family-monospace, monospace);
  color: var(--pico-muted-color);
}

.mod-authors {
  font-size: 0.75rem;
  color: var(--pico-muted-color);
  opacity: 0.8;
}

@media (max-width: 640px) {
  .mods-download {
    margin-left: 0;
    width: 100%;
    justify-content: center;
  }
}
</style>
