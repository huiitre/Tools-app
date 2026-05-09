import { useAlmanaxStore } from "@/modules/Dofus/almanax/almanax.store";
import { useCatalogueStore } from "@/modules/Dofus/catalogue/catalogue.store";
import { useDofusStore } from "@/modules/Dofus/dofus.store"
import { useDofusConfigStore } from "@/modules/Dofus/preferences/preferences.store";

export const resetSessionStores = () => {
  
  useDofusStore().$reset();
  useDofusConfigStore().$reset();
  useCatalogueStore().$reset();
  useAlmanaxStore().$reset();
}