<script setup lang="ts">
import { computed, ref } from 'vue'
import { useRouter } from 'vue-router'
import { storeToRefs } from 'pinia'
import { useWorkshopStore } from '@/modules/Dofus/workshop/store/workshop.store'
import { useWorkshopDetailStore } from '@/modules/Dofus/workshop/store/workshopDetail.store'
import { WorkshopLinkSource, type WorkshopItem } from '@/modules/Dofus/workshop/types/workshop.types'
import { getItemImageByResolution } from '@/modules/Dofus/item/utils/itemImageSelector'
import { AssetResolution } from '@/modules/Dofus/item/types/assetResolution.enum'

const MAX_LINKS = 3

const emit = defineEmits<{ (e: 'close'): void }>()

const router = useRouter()
const workshopStore = useWorkshopStore()
const detailStore = useWorkshopDetailStore()
const { breeds, jobs, tags } = storeToRefs(workshopStore)

//* Tout est préparé ici et n'est envoyé qu'à « Importer » : fermer la popup n'importe rien.
const name = ref(detailStore.name)
const selectedBreedIds = ref<number[]>(detailStore.breeds.map(b => b.id))
const selectedJobIds = ref<number[]>(detailStore.jobs.map(j => j.id))
//* Mes tags uniquement, aucun présélectionné (ceux du propriétaire ne m'appartiennent pas).
const selectedTagIds = ref<number[]>([])

type DraftLink = { key: number; source: WorkshopLinkSource; url: string; label: string }
let nextKey = 0
const links = ref<DraftLink[]>(detailStore.links.map(l => ({ key: nextKey++, source: l.source, url: l.url, label: l.label })))

const toggleIn = (ids: number[], id: number) => ids.includes(id) ? ids.filter(x => x !== id) : [...ids, id]

/* OBJETS */
const items = computed(() => detailStore.items)
const levels = computed(() => items.value.map(i => i.item.level))
const levelLabel = computed(() => {
  if (!levels.value.length) return null
  const min = Math.min(...levels.value)
  const max = Math.max(...levels.value)
  return min === max ? `Niv. ${min}` : `Niv. ${min} – ${max}`
})
const itemImage = (workshopItem: WorkshopItem) =>
  getItemImageByResolution(workshopItem.item.images ?? [], AssetResolution.X2)?.url

/* LIENS */
const editingKey = ref<number | null>(null)
const editUrl = ref('')
const editLabel = ref('')

const startEdit = (link: DraftLink) => {
  showAddForm.value = false
  editingKey.value = link.key
  editUrl.value = link.url
  editLabel.value = link.label
}

const saveEdit = (link: DraftLink) => {
  if (!editUrl.value.trim()) return
  link.url = editUrl.value.trim()
  link.label = editLabel.value.trim()
  editingKey.value = null
}

const removeLink = (key: number) => {
  links.value = links.value.filter(l => l.key !== key)
}

const showAddForm = ref(false)
const newSource = ref<WorkshopLinkSource>(WorkshopLinkSource.DOFUSBOOK)
const newUrl = ref('')

const openAddForm = () => {
  editingKey.value = null
  newSource.value = WorkshopLinkSource.DOFUSBOOK
  newUrl.value = ''
  showAddForm.value = true
}

//* Sans libellé, l'API le déduit de l'URL, comme pour un lien ajouté depuis l'atelier.
const confirmAdd = () => {
  if (!newUrl.value.trim()) return
  links.value.push({ key: nextKey++, source: newSource.value, url: newUrl.value.trim(), label: '' })
  showAddForm.value = false
}

/* IMPORT */
const importing = ref(false)
const error = ref<string | null>(null)

const submit = async () => {
  if (!name.value.trim() || importing.value || !detailStore.workshopId) return
  importing.value = true
  error.value = null
  try {
    const workshop = await workshopStore.importWorkshop(detailStore.workshopId, {
      name: name.value.trim(),
      breedIds: selectedBreedIds.value,
      jobIds: selectedJobIds.value,
      tagIds: selectedTagIds.value,
      links: links.value.map(l => ({ source: l.source, url: l.url, label: l.label || undefined })),
    })
    emit('close')
    router.push({ name: 'dofus-workshop-detail', params: { workshopId: workshop.id } })
  } catch (e: any) {
    //* Nom déjà pris chez moi : on garde la popup ouverte pour le changer.
    error.value = e instanceof Error ? e.message : "Erreur lors de l'import"
  } finally {
    importing.value = false
  }
}
</script>

<template>
  <div class="overlay" @click.self="emit('close')">
    <div class="popup">

      <header class="popup-header">
        <h3>Importer dans mes ateliers</h3>
        <button type="button" class="icon-btn" title="Fermer" @click="emit('close')">
          <i class="mdi mdi-close" />
        </button>
      </header>

      <div class="popup-body">
        <!-- NOM -->
        <label class="section-label" for="import-workshop-name">Nom</label>
        <input
          id="import-workshop-name"
          v-model="name"
          class="field"
          :class="{ invalid: error }"
          placeholder="Nom de l'atelier"
          @input="error = null"
          @keyup.enter="submit"
        />
        <p v-if="error" class="error">{{ error }}</p>

        <!-- OBJETS -->
        <p class="section-label">
          Objets
          <span class="stats">
            <span v-if="levelLabel">{{ levelLabel }}</span>
            <span>{{ items.length }} objet{{ items.length > 1 ? 's' : '' }}</span>
          </span>
        </p>
        <div v-if="items.length" class="items">
          <div
            v-for="workshopItem in items"
            :key="workshopItem.id"
            class="item"
            :title="workshopItem.item.name"
          >
            <div class="item-tile">
              <img v-if="itemImage(workshopItem)" :src="itemImage(workshopItem)" :alt="workshopItem.item.name">
              <span v-if="workshopItem.quantity > 1" class="item-qty">×{{ workshopItem.quantity }}</span>
            </div>
            <span class="item-level">niv. {{ workshopItem.item.level }}</span>
          </div>
        </div>
        <p v-else class="empty">Aucun objet</p>
        <p class="hint">Les objets sont copiés avec leur quantité, en repartant de zéro : rien d'obtenu, aucun sous-craft.</p>

        <!-- CLASSES -->
        <template v-if="breeds.length">
          <p class="section-label">Classes</p>
          <div class="icon-grid">
            <button
              v-for="breed in breeds"
              :key="breed.id"
              type="button"
              class="icon-option"
              :class="{ selected: selectedBreedIds.includes(breed.id) }"
              :title="breed.name"
              @click="selectedBreedIds = toggleIn(selectedBreedIds, breed.id)"
            >
              <img :src="breed.iconUrl ?? undefined" :alt="breed.name">
            </button>
          </div>
        </template>

        <!-- MÉTIERS -->
        <template v-if="jobs.length">
          <p class="section-label">Métiers</p>
          <div class="icon-grid">
            <button
              v-for="job in jobs"
              :key="job.id"
              type="button"
              class="icon-option"
              :class="{ selected: selectedJobIds.includes(job.id) }"
              :title="job.name"
              @click="selectedJobIds = toggleIn(selectedJobIds, job.id)"
            >
              <img :src="job.iconUrl ?? undefined" :alt="job.name">
            </button>
          </div>
        </template>

        <!-- TAGS -->
        <template v-if="tags.length">
          <p class="section-label">Mes tags</p>
          <div class="tag-grid">
            <button
              v-for="tag in tags"
              :key="tag.id"
              type="button"
              class="tag-option"
              :class="{ selected: selectedTagIds.includes(tag.id) }"
              @click="selectedTagIds = toggleIn(selectedTagIds, tag.id)"
            >
              <span class="dot" :style="{ background: tag.color }" />
              {{ tag.name }}
            </button>
          </div>
        </template>

        <!-- LIENS -->
        <p class="section-label">
          Liens
          <span class="stats">{{ links.length }} / {{ MAX_LINKS }}</span>
        </p>
        <div class="links-list">
          <template v-for="link in links" :key="link.key">
            <div v-if="editingKey !== link.key" class="link-row">
              <span class="source-badge">{{ link.source === WorkshopLinkSource.DOFUSBOOK ? 'Dofusbook' : 'Perso' }}</span>
              <div class="link-text">
                <span class="link-label">{{ link.label || 'Libellé automatique' }}</span>
                <span class="link-url">{{ link.url }}</span>
              </div>
              <button type="button" class="icon-btn" title="Modifier le lien" @click="startEdit(link)">
                <i class="mdi mdi-pencil-outline" />
              </button>
              <button type="button" class="icon-btn danger" title="Retirer le lien" @click="removeLink(link.key)">
                <i class="mdi mdi-delete-outline" />
              </button>
            </div>

            <div v-else class="link-form">
              <input v-model="editLabel" class="field" placeholder="Libellé" @keyup.enter="saveEdit(link)" />
              <input v-model="editUrl" class="field" placeholder="URL" @keyup.enter="saveEdit(link)" />
              <div class="form-actions">
                <button type="button" class="btn-ghost" @click="editingKey = null">Annuler</button>
                <button type="button" class="btn-primary" @click="saveEdit(link)">Valider</button>
              </div>
            </div>
          </template>

          <div v-if="showAddForm" class="link-form">
            <div class="add-row">
              <select v-model="newSource" class="field source-select">
                <option :value="WorkshopLinkSource.DOFUSBOOK">Dofusbook</option>
                <option :value="WorkshopLinkSource.CUSTOM">Perso</option>
              </select>
              <input v-model="newUrl" class="field" placeholder="URL" @keyup.enter="confirmAdd" />
            </div>
            <div class="form-actions">
              <button type="button" class="btn-ghost" @click="showAddForm = false">Annuler</button>
              <button type="button" class="btn-primary" @click="confirmAdd">Ajouter</button>
            </div>
          </div>
        </div>

        <button
          v-if="!showAddForm"
          type="button"
          class="add-link-btn"
          :disabled="links.length >= MAX_LINKS"
          @click="openAddForm"
        >
          <i class="mdi mdi-plus" />
          Ajouter un lien
        </button>
      </div>

      <footer class="popup-footer">
        <button
          type="button"
          class="btn-primary"
          :disabled="!name.trim() || importing"
          :aria-busy="importing"
          @click="submit"
        >
          <i class="mdi mdi-download-outline" />
          Importer
        </button>
      </footer>
    </div>
  </div>
</template>

<style scoped lang="scss">
.overlay {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, .5);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
}

.popup {
  display: flex;
  flex-direction: column;
  width: 480px;
  max-width: calc(100vw - 2rem);
  max-height: calc(100vh - 3rem);
  background: var(--pico-card-background-color);
  border: 1px solid var(--pico-card-border-color);
  border-radius: 12px;
  box-shadow: 0 12px 40px rgba(0, 0, 0, .35);
}

.popup-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 1rem 1.1rem 0.6rem;

  h3 {
    margin: 0;
    font-size: 1rem;
    color: var(--pico-contrast);
  }
}

.popup-body {
  flex: 1;
  min-height: 0;
  overflow-y: auto;
  padding: 0 1.1rem 0.5rem;
}

.popup-footer {
  display: flex;
  justify-content: flex-end;
  padding: 0.75rem 1.1rem 1rem;
  border-top: 1px solid var(--pico-muted-border-color);
}

.section-label {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin: 1rem 0 0.45rem;
  font-size: 0.7rem;
  font-weight: 600;
  color: var(--pico-muted-color);
  text-transform: uppercase;
  letter-spacing: 0.06em;

  &:first-child {
    margin-top: 0.25rem;
  }
}

.stats {
  display: flex;
  gap: 0.6rem;
  font-weight: 500;
  letter-spacing: 0;
  text-transform: none;
}

//* Champs compacts : Pico les rend beaucoup trop hauts par défaut.
.field {
  width: 100%;
  height: 2.2rem;
  margin: 0;
  padding: 0 0.7rem;
  font-size: 0.85rem;
  border-radius: 8px;

  &.invalid {
    border-color: #e53e3e;
  }
}

.error {
  margin: 0.35rem 0 0;
  font-size: 0.78rem;
  color: #e53e3e;
}

.hint,
.empty {
  margin: 0.4rem 0 0;
  font-size: 0.72rem;
  color: var(--pico-muted-color);
}

.empty {
  font-style: italic;
}

/* OBJETS */
.items {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(48px, 1fr));
  gap: 6px;
}

.item {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 2px;
}

.item-tile {
  position: relative;
  width: 100%;
  aspect-ratio: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 6px;
  background: color-mix(in srgb, var(--pico-contrast) 7%, transparent);
  border: 1px solid color-mix(in srgb, var(--pico-contrast) 6%, transparent);

  img {
    width: 82%;
    height: 82%;
    object-fit: contain;
  }
}

.item-qty {
  position: absolute;
  right: 2px;
  bottom: 1px;
  font-size: 0.65rem;
  font-weight: 800;
  color: var(--pico-primary);
}

.item-level {
  font-size: 0.62rem;
  color: var(--pico-muted-color);
  white-space: nowrap;
}

/* CLASSES ET MÉTIERS */
.icon-grid {
  display: flex;
  flex-wrap: wrap;
  gap: 0.3rem;
}

.icon-option {
  margin: 0;
  padding: 2px;
  width: auto;
  background: transparent;
  border: 2px solid transparent;
  border-radius: 8px;
  opacity: 0.5;
  cursor: pointer;
  transition: opacity 0.15s, border-color 0.15s;

  img {
    display: block;
    width: 30px;
    height: 30px;
  }

  &:hover {
    opacity: 1;
  }

  &.selected {
    opacity: 1;
    border-color: var(--pico-primary);
  }
}

/* TAGS */
.tag-grid {
  display: flex;
  flex-wrap: wrap;
  gap: 0.35rem;
}

.tag-option {
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
  width: auto;
  margin: 0;
  padding: 0.25rem 0.7rem;
  border-radius: 999px;
  border: 1px solid var(--pico-card-border-color);
  background: transparent;
  color: var(--pico-muted-color);
  font-size: 0.78rem;
  cursor: pointer;

  &:hover {
    border-color: var(--pico-primary);
    color: var(--pico-contrast);
  }

  &.selected {
    border-color: var(--pico-primary);
    color: var(--pico-primary);
    font-weight: 700;
  }
}

.dot {
  width: 0.6rem;
  height: 0.6rem;
  border-radius: 50%;
}

/* LIENS */
.links-list {
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
  margin-bottom: 0.5rem;
}

.link-row {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.45rem 0.4rem 0.45rem 0.55rem;
  border-radius: 8px;
  background: color-mix(in srgb, var(--pico-contrast) 5%, transparent);
}

.source-badge {
  flex-shrink: 0;
  padding: 0.1rem 0.45rem;
  border-radius: 999px;
  background: color-mix(in srgb, var(--pico-primary) 18%, transparent);
  color: var(--pico-primary);
  font-size: 0.65rem;
  font-weight: 700;
}

.link-text {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  line-height: 1.25;
}

.link-label,
.link-url {
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.link-label {
  font-size: 0.85rem;
  color: var(--pico-contrast);
}

.link-url {
  font-size: 0.7rem;
  color: var(--pico-muted-color);
}

.link-form {
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
  padding: 0.55rem;
  border-radius: 8px;
  border: 1px solid var(--pico-card-border-color);
}

.add-row {
  display: flex;
  gap: 0.4rem;
}

.source-select {
  width: auto;
  flex-shrink: 0;
  padding-right: 2rem;
}

.form-actions {
  display: flex;
  justify-content: flex-end;
  gap: 0.4rem;
}

.add-link-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.35rem;
  width: 100%;
  margin: 0;
  padding: 0.45rem;
  font-size: 0.8rem;
  background: transparent;
  border: 1px dashed var(--pico-muted-border-color);
  border-radius: 8px;
  color: var(--pico-muted-color);
  cursor: pointer;

  &:hover:not(:disabled) {
    border-color: var(--pico-primary);
    color: var(--pico-primary);
  }

  &:disabled {
    opacity: 0.4;
    cursor: not-allowed;
  }
}

/* BOUTONS */
.icon-btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  width: 1.9rem;
  height: 1.9rem;
  margin: 0;
  padding: 0;
  border: none;
  border-radius: 8px;
  background: transparent;
  color: var(--pico-muted-color);
  font-size: 1.05rem;
  cursor: pointer;

  &:hover {
    background: color-mix(in srgb, var(--pico-contrast) 10%, transparent);
    color: var(--pico-contrast);
  }

  &.danger:hover {
    color: #e53e3e;
  }
}

.btn-primary,
.btn-ghost {
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
  width: auto;
  margin: 0;
  padding: 0.4rem 1rem;
  font-size: 0.82rem;
  border-radius: 8px;
}

.btn-ghost {
  background: transparent;
  border: 1px solid var(--pico-muted-border-color);
  color: inherit;

  &:hover {
    border-color: var(--pico-contrast);
  }
}
</style>
