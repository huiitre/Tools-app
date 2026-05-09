import { Workshop, WorkshopTag } from "@/modules/Dofus/workshop/types/workshop.types"
import { defineStore } from "pinia"
import { 
  useFetchWorkshops, 
  useCreateWorkshop, 
  useUpdateWorkshop, 
  useDeleteWorkshop
} from "@/modules/Dofus/workshop/fetch/workshop.fetch"
import { 
  useFetchWorkshopTags, 
  useCreateWorkshopTag, 
  useUpdateWorkshopTag, 
  useDeleteWorkshopTag,
  useAddTagsToWorkshop,
  useRemoveTagFromWorkshop
} from "@/modules/Dofus/workshop/fetch/workshopTag.fetch"

type WorkshopState = {
  workshops: Workshop[]
  tags: WorkshopTag[]
}

export const useWorkshopStore = defineStore('dofus.workshop', {
  state: (): WorkshopState => ({
    workshops: [],
    tags: []
  }),

  getters: {
    activeWorkshops: (state) => state.workshops.filter(w => w.active),
    
    archivedWorkshops: (state) => state.workshops.filter(w => !w.active),
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

    // Workshop CRUD
    async fetchWorkshops() {
      const response = await useFetchWorkshops()
      this.workshops = this.sortWorkshops(response.data)
    },

    async createWorkshop(name: string, tagIds?: number[]) {
      const response = await useCreateWorkshop({ name, tagIds })
      this.workshops.push(response.data)
      this.workshops = this.sortWorkshops(this.workshops)
      return response.data
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
      
      this.workshops.forEach(workshop => {
        workshop.tags = workshop.tags.filter(t => t.id !== id)
      })
    }
  }
})