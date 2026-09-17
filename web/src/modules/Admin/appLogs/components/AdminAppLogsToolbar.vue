<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from "vue";
import { useFloating, offset } from "@floating-ui/vue";
import { useAdminAppLogsStore } from "../store/adminAppLogs.store";
import { APP_LOG_PAGE_SIZES } from "../types/adminAppLogs.types";
import AppLogFilterSelectorModal from "./AppLogFilterSelectorModal.vue";

const store = useAdminAppLogsStore();
const selector = ref<
  "users" | "modules" | "areas" | "actions" | "presets" | null
>(null);
const selectedPreset = ref<string | null>(null);
const presetOptions = [
  { id: "none", label: "Aucune période" },
  { id: "today", label: "Aujourd’hui" },
  { id: "yesterday", label: "Hier" },
  { id: "this-week", label: "Cette semaine" },
  { id: "last-week", label: "La semaine dernière" },
  { id: "this-month", label: "Ce mois-ci" },
  { id: "last-month", label: "Le mois dernier" },
  { id: "this-year", label: "Cette année" },
  { id: "last-year", label: "L’année dernière" },
  { id: "last-7-days", label: "Les 7 derniers jours" },
  { id: "last-30-days", label: "Les 30 derniers jours" },
];
const searchValue = ref(store.search ?? "");
let debounce: ReturnType<typeof setTimeout> | null = null;
const selectorOptions = computed(() => {
  if (selector.value === "users")
    return store.users.map((user) => ({
      id: user.id,
      label: user.name,
      detail: `${user.email} · ${
        store.roleOf(user.roleId)?.name ?? "Sans rôle"
      }`,
    }));
  if (selector.value === "modules")
    return store.modules.map((module) => ({
      id: String(module.id),
      label: module.name,
      detail: module.code,
    }));
  if (selector.value === "areas")
    return store.areaCodes.map((code) => ({ id: code, label: code }));
  if (selector.value === "actions")
    return store.actionCodes.map((code) => ({ id: code, label: code }));
  if (selector.value === "presets") return presetOptions;
  return [];
});
const selectorTitle = computed(
  () =>
    ({
      users: "Utilisateurs",
      modules: "Modules",
      areas: "Zones",
      actions: "Actions",
      presets: "Période prédéfinie",
    }[selector.value ?? "users"])
);
const selectorValue = computed(() =>
  selector.value === "users"
    ? store.userIds.map(String)
    : selector.value === "modules"
    ? store.moduleIds.map(String)
    : selector.value === "areas"
    ? store.selectedAreaCodes
    : selector.value === "actions"
    ? store.selectedActionCodes
    : selector.value === "presets" && selectedPreset.value
    ? [selectedPreset.value]
    : []
);
watch(searchValue, (value) => {
  if (debounce) clearTimeout(debounce);
  debounce = setTimeout(() => void store.setSearch(value.trim() || null), 300);
});
function applySelection(values: string[]) {
  const current = selector.value;
  selector.value = null;
  if (current === "presets") {
    const preset = values[0] ?? "none";
    selectedPreset.value = preset === "none" ? null : preset;
    const [from, to] = getPresetRange(preset);
    void store.setDateRange(from, to);
    return;
  }
  if (current === "users") void store.setUserIds(values.map(Number));
  if (current === "modules") void store.setModuleIds(values.map(Number));
  if (current === "areas") void store.setAreaCodes(values);
  if (current === "actions") void store.setActionCodes(values);
}
function dateOnly(date: Date) {
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(
    2,
    "0"
  )}-${String(date.getDate()).padStart(2, "0")}`;
}
function getPresetRange(preset: string): [string | null, string | null] {
  const today = new Date();
  today.setHours(0, 0, 0, 0);
  const start = new Date(today);
  const end = new Date(today);
  if (preset === "none") return [null, null];
  if (preset === "yesterday") {
    start.setDate(start.getDate() - 1);
    end.setDate(end.getDate() - 1);
  } else if (preset === "this-week" || preset === "last-week") {
    const mondayOffset = (today.getDay() + 6) % 7;
    start.setDate(start.getDate() - mondayOffset);
    if (preset === "last-week") start.setDate(start.getDate() - 7);
    end.setTime(start.getTime());
    end.setDate(end.getDate() + 6);
  } else if (preset === "this-month" || preset === "last-month") {
    start.setDate(1);
    if (preset === "last-month") {
      start.setMonth(start.getMonth() - 1);
      end.setTime(start.getTime());
      end.setMonth(end.getMonth() + 1);
      end.setDate(0);
    }
  } else if (preset === "this-year" || preset === "last-year") {
    start.setMonth(0, 1);
    if (preset === "last-year") {
      start.setFullYear(start.getFullYear() - 1);
      end.setTime(start.getTime());
      end.setFullYear(end.getFullYear() + 1);
      end.setDate(0);
    }
  } else if (preset === "last-7-days") start.setDate(start.getDate() - 6);
  else if (preset === "last-30-days") start.setDate(start.getDate() - 29);
  return [dateOnly(start), dateOnly(end)];
}
const presetLabel = computed(
  () =>
    presetOptions.find((option) => option.id === selectedPreset.value)?.label ??
    "Période"
);
async function clearAllFilters() {
  searchValue.value = "";
  selectedPreset.value = null;
  await store.clearFilters();
}
const goFirst = () => void store.setPage(1);
const goPrevious = () => store.page > 1 && void store.setPage(store.page - 1);
const goNext = () =>
  store.page < store.lastPage && void store.setPage(store.page + 1);
const goLast = () => void store.setPage(store.lastPage);
const isPreferencesOpen = ref(false);
const preferencesRef = ref<HTMLElement | null>(null);
const preferencesFloating = ref<HTMLElement | null>(null);
const { floatingStyles } = useFloating(preferencesRef, preferencesFloating, {
  placement: "bottom-start",
  middleware: [offset(6)],
});
const onClickOutside = (event: MouseEvent) => {
  const target = event.target as HTMLElement;
  if (
    preferencesRef.value &&
    preferencesFloating.value &&
    !preferencesRef.value.contains(target) &&
    !preferencesFloating.value.contains(target)
  )
    isPreferencesOpen.value = false;
};
const onScroll = () => {
  isPreferencesOpen.value = false;
};
onMounted(() => {
  document.addEventListener("click", onClickOutside);
  document.addEventListener("scroll", onScroll, true);
});
onBeforeUnmount(() => {
  if (debounce) clearTimeout(debounce);
  document.removeEventListener("click", onClickOutside);
  document.removeEventListener("scroll", onScroll, true);
});
</script>

<template>
  <div class="logs-toolbar">
    <div class="toolbar-left">
      <button class="icon" :disabled="store.page === 1" @click="goFirst">
        <i class="mdi mdi-page-first" /></button
      ><button class="icon" :disabled="store.page === 1" @click="goPrevious">
        <i class="mdi mdi-chevron-left" />
      </button>
      <span class="page-indicator"
        >Page <strong>{{ store.page }}</strong> / {{ store.lastPage }}
        <span class="total-hint">({{ store.totalCount }} logs)</span></span
      >
      <button
        class="icon"
        :disabled="store.page >= store.lastPage"
        @click="goNext"
      >
        <i class="mdi mdi-chevron-right" /></button
      ><button
        class="icon"
        :disabled="store.page >= store.lastPage"
        @click="goLast"
      >
        <i class="mdi mdi-page-last" />
      </button>
      <button
        ref="preferencesRef"
        class="icon pref-btn"
        @click="isPreferencesOpen = !isPreferencesOpen"
      >
        <i class="mdi mdi-tune" />
      </button>
      <div
        v-if="isPreferencesOpen"
        ref="preferencesFloating"
        class="floating-panel"
        :style="floatingStyles"
      >
        <div class="pref-panel">
          <h3 class="panel-title">Journal applicatif</h3>
          <div class="pref-block">
            <label class="pref-label">Taille de page</label
            ><select
              class="pref-select"
              :value="store.pageSize"
              @change="
                store.setPageSize(
                  Number(($event.target as HTMLSelectElement).value) as any
                )
              "
            >
              <option
                v-for="size in APP_LOG_PAGE_SIZES"
                :key="size"
                :value="size"
              >
                {{ size }} lignes
              </option>
            </select>
          </div>
          <div class="pref-block">
            <label class="pref-label">Colonnes</label>
            <div
              v-for="column in store.columns.filter(
                (column) => column.userToggle
              )"
              :key="column.key"
              class="pref-switch"
            >
              <span>{{ column.label }}</span
              ><label
                ><input
                  type="checkbox"
                  role="switch"
                  :checked="column.visible"
                  @change="store.toggleColumn(column.key)"
              /></label>
            </div>
          </div>
        </div>
      </div>
      <span class="toolbar-separator" />
      <button type="button" class="filter-button" @click="selector = 'users'">
        <i class="mdi mdi-account-multiple-outline" /> Utilisateurs
        <b v-if="store.userIds.length">{{ store.userIds.length }}</b>
      </button>
      <button type="button" class="filter-button" @click="selector = 'modules'">
        <i class="mdi mdi-view-grid-outline" /> Modules
        <b v-if="store.moduleIds.length">{{ store.moduleIds.length }}</b>
      </button>
      <button type="button" class="filter-button" @click="selector = 'areas'">
        <i class="mdi mdi-map-marker-outline" /> Zones
        <b v-if="store.selectedAreaCodes.length">{{
          store.selectedAreaCodes.length
        }}</b>
      </button>
      <button type="button" class="filter-button" @click="selector = 'actions'">
        <i class="mdi mdi-lightning-bolt-outline" /> Actions
        <b v-if="store.selectedActionCodes.length">{{
          store.selectedActionCodes.length
        }}</b>
      </button>
      <button
        type="button"
        class="icon clear-button"
        :disabled="!store.hasFilters"
        title="Effacer les filtres"
        @click="clearAllFilters"
      >
        <i class="mdi mdi-filter-remove-outline" />
      </button>
    </div>
    <div class="toolbar-right">
      <button
        type="button"
        class="filter-button period-button"
        @click="selector = 'presets'"
      >
        <i class="mdi mdi-calendar-range-outline" /> {{ presetLabel }}
      </button>
      <input
        :value="store.createdFrom ?? ''"
        type="date"
        aria-label="Logs à partir du"
        @change="
          selectedPreset = null;
          store.setCreatedFrom(
            ($event.target as HTMLInputElement).value || null
          );
        "
      /><input
        :value="store.createdTo ?? ''"
        type="date"
        aria-label="Logs jusqu’au"
        @change="
          selectedPreset = null;
          store.setCreatedTo(($event.target as HTMLInputElement).value || null);
        "
      /><input
        v-model="searchValue"
        type="search"
        class="search-input"
        placeholder="Nom, e-mail, IP ou user-agent…"
      />
    </div>
  </div>
  <AppLogFilterSelectorModal
    :open="selector !== null"
    :title="selectorTitle"
    :options="selectorOptions"
    :model-value="selectorValue"
    :multiple="selector !== 'presets'"
    @apply="applySelection"
    @close="selector = null"
  />
</template>

<style scoped lang="scss">
.logs-toolbar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 1rem;
  padding: 0.25rem 0;
}
.toolbar-left,
.toolbar-right {
  display: flex;
  align-items: center;
  gap: 0.4rem;
}
.toolbar-left {
  flex-wrap: wrap;
}
button {
  background: none;
  border: none;
  color: var(--pico-muted-color);
  cursor: pointer;
  &:disabled {
    opacity: 0.35;
    cursor: not-allowed;
  }
  &:hover:not(:disabled) {
    color: var(--pico-primary);
  }
}
button.icon {
  padding: 0.25rem;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  i {
    font-size: 1.1rem;
  }
}
.pref-btn {
  margin-left: 0.5rem;
}
.clear-button {
  display: grid !important;
  place-items: center;
  width: 2rem;
  height: 2rem;
  padding: 0 !important;
  margin: 0 !important;
  align-self: center;
}
.clear-button i {
  line-height: 1;
}
.page-indicator {
  margin: 0 0.5rem;
  font-size: 0.85rem;
  color: var(--pico-muted-color);
  user-select: none;
}
.total-hint {
  font-size: 0.78rem;
  opacity: 0.7;
  margin-left: 0.25rem;
}
.toolbar-separator {
  height: 1.25rem;
  border-left: 1px solid var(--pico-muted-border-color);
  margin: 0 0.15rem;
}
.filter-button {
  display: inline-flex;
  align-items: center;
  align-self: center;
  gap: 0.35rem;
  width: auto;
  height: 2rem;
  box-sizing: border-box;
  padding-block: 0 !important;
  padding-inline: 0.7rem;
  margin-block: 0 !important;
  line-height: 1;
  border: 1px solid var(--pico-form-element-border-color);
  border-radius: var(--pico-border-radius);
  background: var(--pico-form-element-background-color);
  color: var(--pico-form-element-color);
  font-size: 0.75rem;
  white-space: nowrap;
  i {
    font-size: 0.9rem;
  }
  b {
    display: grid;
    place-items: center;
    min-width: 1rem;
    height: 1rem;
    padding: 0 0.2rem;
    border-radius: 99px;
    background: var(--pico-primary);
    color: var(--pico-primary-inverse);
    font-size: 0.62rem;
  }
  &:hover {
    border-color: var(--pico-primary);
    color: var(--pico-primary);
  }
}
.toolbar-right input[type="date"] {
  width: auto;
  height: 2rem;
  margin: 0;
  font-size: 0.75rem;
}
.search-input {
  width: 220px;
  height: 2rem;
  margin: 0;
  font-size: 0.75rem;
}
.floating-panel {
  position: absolute;
  z-index: 1000;
  background: var(--pico-background-color);
  border: 1px solid var(--pico-muted-border-color);
  border-radius: var(--pico-border-radius);
  box-shadow: var(--pico-card-box-shadow);
}
.pref-panel {
  padding: 0.65rem 0.75rem;
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  min-width: 220px;
  font-size: 0.75rem;
}
.panel-title {
  font-size: 1rem;
  font-weight: 600;
  margin: 0;
}
.pref-block {
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
  align-items: flex-start;
}
.pref-label {
  font-weight: 500;
  color: var(--pico-primary);
}
.pref-select {
  width: auto;
  min-width: 130px;
  height: 2rem;
  margin: 0;
  padding: 0 2rem 0 0.5rem;
  font-size: 0.75rem;
}
.pref-switch {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 0.5rem;
  width: 100%;
}
@media (max-width: 1100px) {
  .logs-toolbar {
    align-items: flex-start;
    flex-direction: column;
  }
  .toolbar-right {
    width: 100%;
    flex-wrap: wrap;
  }
}
</style>
