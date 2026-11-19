import { Workshop, WorkshopBreed, WorkshopJob, WorkshopTag } from "@/modules/Dofus/workshop/types/workshop.types"
import { defineStore } from "pinia"
import { useItemPrices } from "@/modules/Dofus/almanax/composables/useItemPrices"
import { 
  useFetchWorkshops, 
  useCreateWorkshop, 
  useUpdateWorkshop, 
  useDeleteWorkshop,
  useImportWorkshop,
  type ImportWorkshopBody
} from "@/modules/Dofus/workshop/fetch/workshop.fetch"
import { 
  useFetchWorkshopTags, 
  useCreateWorkshopTag, 
  useUpdateWorkshopTag, 
  useDeleteWorkshopTag,
  useAddTagsToWorkshop,
  useRemoveTagFromWorkshop
} from "@/modules/Dofus/workshop/fetch/workshopTag.fetch"
import {
  useFetchWorkshopBreeds,
  useAddBreedsToWorkshop,
  useRemoveBreedFromWorkshop
} from "@/modules/Dofus/workshop/fetch/workshopBreed.fetch"
import {
  useFetchWorkshopJobs,
  useAddJobsToWorkshop,
  useRemoveJobFromWorkshop
} from "@/modules/Dofus/workshop/fetch/workshopJob.fetch"

export type WorkshopSortKey = 'name'
export type WorkshopSortDir = 'asc' | 'desc'

const STORAGE_KEY_SORT = 'dofus.workshop.sort'

const readSort = (): { sortKey: WorkshopSortKey; sortDir: WorkshopSortDir } => {
  const fallback = { sortKey: 'name' as const, sortDir: 'asc' as const }
  try {
    const raw = localStorage.getItem(STORAGE_KEY_SORT)
    return raw ? { ...fallback, ...JSON.parse(raw) } : fallback
  } catch (e) {
    console.warn('Failed to hydrate workshop sort', e)
    return fallback
  }
}

const toggled = (ids: Set<number>, id: number) => {
  const next = new Set(ids)
  if (next.has(id)) next.delete(id)
  else next.add(id)
  return next
}

type WorkshopState = {
  workshops: Workshop[]
  tags: WorkshopTag[]
  //* Catalogues des icônes système proposables sur un atelier.
  breeds: WorkshopBreed[]
  jobs: WorkshopJob[]
  //* Filtre de la liste : gardé en mémoire le temps de la session, pas en localStorage.
  selectedTagIds: Set<number>
  selectedBreedIds: Set<number>
  selectedJobIds: Set<number>
  sortKey: WorkshopSortKey
  sortDir: WorkshopSortDir
  //* Atelier tout juste créé, affiché en tête de liste jusqu'à ce qu'on la quitte.
  newWorkshopId: number | null
}

export const useWorkshopStore = defineStore('dofus.workshop', {
  state: (): WorkshopState => ({
    workshops: [],
    tags: [],
    breeds: [],
    jobs: [],
    selectedTagIds: new Set(),
    selectedBreedIds: new Set(),
    selectedJobIds: new Set(),
    ...readSort(),
    newWorkshopId: null
  }),

  getters: {
    activeWorkshops: (state) => state.workshops.filter(w => w.active),
    
    archivedWorkshops: (state) => state.workshops.filter(w => !w.active),

    hasFilters: (state) => state.selectedTagIds.size + state.selectedBreedIds.size + state.selectedJobIds.size > 0,
  },

  actions: {
    sortWorkshops(workshops: Workshop[]) {
      return workshops.sort((a, b) => {
        // D'abord par pinned (true avant false)
        if (a.pinned !== b.pinned) {
          return a.pinned ? -1 : 1
        }
        // Ensuite par nom alphabétique
        return a.name.localeCompare(b.name)
      })
    },

    setSort(sortKey: WorkshopSortKey, sortDir: WorkshopSortDir) {
      this.sortKey = sortKey
      this.sortDir = sortDir
      localStorage.setItem(STORAGE_KEY_SORT, JSON.stringify({ sortKey, sortDir }))
    },

    toggleSelectedTag(tagId: number) {
      this.selectedTagIds = toggled(this.selectedTagIds, tagId)
    },

    toggleSelectedBreed(breedId: number) {
      this.selectedBreedIds = toggled(this.selectedBreedIds, breedId)
    },

    toggleSelectedJob(jobId: number) {
      this.selectedJobIds = toggled(this.selectedJobIds, jobId)
    },

    clearFilters() {
      this.selectedTagIds = new Set()
      this.selectedBreedIds = new Set()
      this.selectedJobIds = new Set()
    },

    //* Classes et métiers qui n'existent pas dans la version du jeu affichée sortent du filtre.
    pruneSelectedBreedsAndJobs() {
      const breedIds = new Set(this.breeds.map(b => b.id))
      const jobIds = new Set(this.jobs.map(j => j.id))
      this.selectedBreedIds = new Set([...this.selectedBreedIds].filter(id => breedIds.has(id)))
      this.selectedJobIds = new Set([...this.selectedJobIds].filter(id => jobIds.has(id)))
    },

    replaceWorkshop(workshop: Workshop) {
      const index = this.workshops.findIndex(w => w.id === workshop.id)
      if (index !== -1) this.workshops[index] = workshop
    },

    //* Retire de la sélection les tags qui n'existent plus (supprimés, autre compte).
    pruneSelectedTags() {
      const existing = new Set(this.tags.map(t => t.id))
      this.selectedTagIds = new Set([...this.selectedTagIds].filter(id => existing.has(id)))
    },

    // Workshop CRUD
    async fetchWorkshops() {
      const response = await useFetchWorkshops()
      this.workshops = this.sortWorkshops(response.data)

      //* Prix des objets et ingrédients de tous les ateliers, pour les chiffres des cartes (seuls ceux absents du cache partent).
      const itemIds = new Set(this.workshops.flatMap(w => w.items.flatMap(i => [i.item.id, ...i.ingredients.map(g => g.item.id)])))
      await useItemPrices().load([...itemIds])
    },

    async createWorkshop(name: string, tagIds?: number[]) {
      const response = await useCreateWorkshop({ name, tagIds })
      this.workshops.push(response.data)
      this.workshops = this.sortWorkshops(this.workshops)
      this.newWorkshopId = response.data.id
      return response.data
    },

    //* Copie l'atelier d'un autre utilisateur : il arrive dans la liste comme un atelier tout juste créé.
    async importWorkshop(sourceWorkshopId: number, body: ImportWorkshopBody) {
      const response = await useImportWorkshop(sourceWorkshopId, body)
      this.workshops.push(response.data)
      this.workshops = this.sortWorkshops(this.workshops)
      this.newWorkshopId = response.data.id
      const workshop = response.data as Workshop
      await useItemPrices().load(workshop.items.flatMap(i => [i.item.id, ...i.ingredients.map(g => g.item.id)]))
      return workshop
    },

    async updateWorkshop(id: number, data: { name?: string; active?: boolean; tagIds?: number[], pinned?: boolean }) {
      const response = await useUpdateWorkshop(id, data)
      const index = this.workshops.findIndex(w => w.id === id)
      if (index !== -1) {
        this.workshops[index] = response.data
      }

      this.workshops = this.sortWorkshops(this.workshops)

      return response.data
    },

    async addTagsToWorkshop(workshopId: number, tagIds: number[]) {
      await useAddTagsToWorkshop(workshopId, tagIds)

      const workshop = this.workshops.find(w => w.id === workshopId)
      if (!workshop) return

      const tagsToAdd = this.tags.filter(t => tagIds.includes(t.id))

      workshop.tags = [
        ...workshop.tags.filter(t => !tagIds.includes(t.id)),
        ...tagsToAdd
      ]
    },

    async removeTagFromWorkshop(workshopId: number, tagId: number) {
      await useRemoveTagFromWorkshop(workshopId, tagId)

      const workshop = this.workshops.find(w => w.id === workshopId)
      if (!workshop) return

      workshop.tags = workshop.tags.filter(t => t.id !== tagId)
    },

    async deleteWorkshop(id: number) {
      await useDeleteWorkshop(id)
      this.workshops = this.workshops.filter(w => w.id !== id)
    },

    // Tag CRUD
    async fetchTags() {
      const response = await useFetchWorkshopTags()
      this.tags = response.data
      this.pruneSelectedTags()
    },

    async createTag(name: string, color: string) {
      const response = await useCreateWorkshopTag({ name, color })
      this.tags.push(response.data)
      return response.data
    },

    async updateTag(id: number, name: string, color: string) {
      const response = await useUpdateWorkshopTag(id, { name, color })
      
      this.tags = this.tags.map(t => t.id === id ? response.data : t)
      
      this.workshops = this.workshops.map(w => ({
        ...w,
        tags: w.tags.map(t => t.id === id ? response.data : t)
      }))
      
      return response.data
    },

    async deleteTag(id: number) {
      await useDeleteWorkshopTag(id)
      this.tags = this.tags.filter(t => t.id !== id)
      this.pruneSelectedTags()

      this.workshops.forEach(workshop => {
        workshop.tags = workshop.tags.filter(t => t.id !== id)
      })
    },

    // Classes et métiers
    async fetchBreeds() {
      const response = await useFetchWorkshopBreeds()
      this.breeds = response.data
      this.pruneSelectedBreedsAndJobs()
    },

    async fetchJobs() {
      const response = await useFetchWorkshopJobs()
      this.jobs = response.data
      this.pruneSelectedBreedsAndJobs()
    },

    async addBreedsToWorkshop(workshopId: number, breedIds: number[]) {
      const response = await useAddBreedsToWorkshop(workshopId, breedIds)
      this.replaceWorkshop(response.data)
    },

    async removeBreedFromWorkshop(workshopId: number, breedId: number) {
      await useRemoveBreedFromWorkshop(workshopId, breedId)

      const workshop = this.workshops.find(w => w.id === workshopId)
      if (workshop) workshop.breeds = workshop.breeds.filter(b => b.id !== breedId)
    },

    async addJobsToWorkshop(workshopId: number, jobIds: number[]) {
      const response = await useAddJobsToWorkshop(workshopId, jobIds)
      this.replaceWorkshop(response.data)
    },

    async removeJobFromWorkshop(workshopId: number, jobId: number) {
      await useRemoveJobFromWorkshop(workshopId, jobId)

      const workshop = this.workshops.find(w => w.id === workshopId)
      if (workshop) workshop.jobs = workshop.jobs.filter(j => j.id !== jobId)
    },
  }
})