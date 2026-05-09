import { defineStore } from "pinia";

type UIState = {
  isLoading: boolean;
};

export const useUIStore = defineStore("ui", {
  state: (): UIState => ({
    isLoading: false,
  }),

  actions: {
    setLoading(loading: boolean) {
      this.isLoading = loading;
    },
  },
});