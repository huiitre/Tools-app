import { CatalogueSearch } from '@/modules/Dofus/catalogue/types/catalogue.types'
import { clientV3Dofus } from '@/services/axiosInstance'

export const useFetchColumns = async () => {
  return await clientV3Dofus.get('/dofus/catalogue/columns')
}

export const useFetchSearch = async(data: CatalogueSearch) => {
  return await clientV3Dofus.get('/dofus/catalogue/search', {
    params: {
      q: data.q,
      page: data.page,
      pageSize: data.pageSize,
      sort: data.sort,
      dir: data.dir,
    }
  })
}

export const useFetchRecipeByItemId = async(itemId: number) => {
  return await clientV3Dofus.get(`/dofus/catalogue/item/${itemId}/recipe`)
}