import { useAlmanaxStore } from "@/modules/Dofus/almanax/almanax.store";
import { useCatalogueStore } from "@/modules/Dofus/catalogue/catalogue.store";
import { useDofusStore } from "@/modules/Dofus/dofus.store"
import { useDofusConfigStore } from "@/modules/Dofus/preferences/preferences.store";
import { useRiotStore } from "@/modules/Riot/riot.store";
import { useGameServersStore } from "@/modules/Core/GameServers/store/gameServers.store";

export const resetSessionStores = () => {

  useDofusStore().$reset();
  useDofusConfigStore().$reset();
  useCatalogueStore().$reset();
  useAlmanaxStore().$reset();
  useRiotStore().clearAll();

  // unsubscribeLive() avant $reset() : l'abonnement au hub vit hors du state Pinia, $reset()
  // seul ne le couperait pas et en perdrait la référence.
  const gameServers = useGameServersStore();
  gameServers.unsubscribeLive();
  gameServers.$reset();
}