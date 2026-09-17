<script setup lang="ts">
import { computed, ref, watch } from "vue";

type Option = { id: string; label: string; detail?: string };

const props = defineProps<{
  open: boolean;
  title: string;
  options: Option[];
  modelValue: string[];
  multiple?: boolean;
}>();

const emit = defineEmits<{ apply: [ids: string[]]; close: [] }>();

const query = ref("");
const pendingIds = ref<string[]>([]);
const visibleOptions = computed(() => {
  const normalized = query.value.trim().toLocaleLowerCase();
  return props.options.filter(
    (option) =>
      !normalized ||
      `${option.label} ${option.detail ?? ""}`
        .toLocaleLowerCase()
        .includes(normalized)
  );
});

function toggle(id: string) {
  if (props.multiple === false) {
    emit("apply", [id]);
    return;
  }
  pendingIds.value = pendingIds.value.includes(id)
    ? pendingIds.value.filter((value) => value !== id)
    : [...pendingIds.value, id];
}

watch(
  () => props.open,
  (open) => {
    if (!open) return;
    pendingIds.value = [...props.modelValue];
    query.value = "";
  }
);
</script>

<template>
  <Teleport to="body">
    <Transition name="filter-modal">
      <div
        v-if="open"
        class="filter-modal-backdrop"
        @mousedown.self="emit('close')"
      >
        <section
          class="filter-modal"
          role="dialog"
          aria-modal="true"
          :aria-label="title"
        >
          <header>
            <div>
              <strong>{{ title }}</strong>
              <small>{{
                props.multiple === false
                  ? "Sélectionne une période."
                  : "Sélection multiple possible."
              }}</small>
            </div>
            <div class="header-actions">
              <span>{{ pendingIds.length }}</span>
              <button type="button" aria-label="Fermer" @click="emit('close')">
                <i class="mdi mdi-close" />
              </button>
            </div>
          </header>

          <div class="modal-toolbar">
            <label :for="`${title}-search`">Rechercher</label>
            <button
              type="button"
              :disabled="pendingIds.length === 0"
              @click="pendingIds = []"
            >
              Effacer
            </button>
          </div>
          <div class="modal-search">
            <i class="mdi mdi-magnify" />
            <input
              :id="`${title}-search`"
              v-model="query"
              type="search"
              :placeholder="`Rechercher…`"
            />
          </div>

          <div class="modal-list custom-scrollbar">
            <button
              v-for="option in visibleOptions"
              :key="option.id"
              type="button"
              class="option"
              :class="{ selected: pendingIds.includes(option.id) }"
              @click="toggle(option.id)"
            >
              <span
                ><b>{{ option.label }}</b
                ><small v-if="option.detail">{{ option.detail }}</small></span
              >
              <i v-if="pendingIds.includes(option.id)" class="mdi mdi-check" />
            </button>
            <p v-if="visibleOptions.length === 0">Aucun résultat.</p>
          </div>

          <footer>
            <button type="button" @click="emit('apply', pendingIds)">
              Valider
            </button>
          </footer>
        </section>
      </div>
    </Transition>
  </Teleport>
</template>

<style scoped lang="scss">
.filter-modal-backdrop {
  position: fixed;
  z-index: 1100;
  inset: 0;
  display: grid;
  place-items: center;
  padding: 1rem;
  background: rgb(0 0 0 / 58%);
  backdrop-filter: blur(3px);
}
.filter-modal {
  display: flex;
  flex-direction: column;
  width: min(560px, 100%);
  max-height: min(640px, calc(100vh - 2rem));
  padding: 1rem;
  margin: 0;
  overflow: hidden;
  background: var(--pico-card-background-color);
  border: 1px solid var(--pico-muted-border-color);
  border-radius: 12px;
  box-shadow: 0 18px 50px rgb(0 0 0 / 42%);
}
header,
.header-actions,
.modal-toolbar,
footer {
  display: flex;
  align-items: center;
}
header {
  justify-content: space-between;
  gap: 1rem;
}
header strong,
header small,
.option small {
  display: block;
}
header strong {
  font-size: 0.86rem;
}
header small,
.option small {
  margin-top: 0.2rem;
  color: var(--pico-muted-color);
  font-size: 0.66rem;
}
.header-actions {
  gap: 0.65rem;
}
.header-actions span {
  padding: 0.15rem 0.42rem;
  border-radius: 999px;
  background: color-mix(in srgb, var(--pico-primary) 16%, transparent);
  color: var(--pico-primary);
  font-size: 0.65rem;
  font-weight: 700;
}
.header-actions button {
  display: grid;
  place-items: center;
  width: 2rem;
  height: 2rem;
  margin: 0;
  padding: 0;
  border-color: var(--pico-muted-border-color);
  background: transparent;
  color: var(--pico-muted-color);
}
.modal-toolbar {
  justify-content: space-between;
  margin-top: 1rem;
  font-size: 0.72rem;
  font-weight: 600;
}
.modal-toolbar button {
  width: auto;
  margin: 0;
  padding: 0;
  border: 0;
  background: transparent;
  color: var(--pico-muted-color);
  font-size: 0.65rem;
}
.modal-search {
  display: flex;
  align-items: center;
  gap: 0.45rem;
  margin-top: 0.45rem;
  padding: 0 0.6rem;
  border: 1px solid var(--pico-muted-border-color);
  border-radius: var(--pico-border-radius);
  color: var(--pico-muted-color);
}
.modal-search input {
  height: 2.15rem;
  margin: 0;
  padding: 0;
  border: 0;
  background: transparent;
  background-image: none;
  box-shadow: none;
  font-size: 0.78rem;
}
.modal-list {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 0.5rem 1.4rem;
  min-height: 0;
  margin-top: 0.7rem;
  overflow-y: auto;
  padding: 0.15rem 0.25rem 0.15rem 0.15rem;
}
.option {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 0.35rem;
  width: 100%;
  min-height: 38px;
  margin: 0;
  padding: 0.35rem 0.5rem;
  border: 1px solid var(--pico-muted-border-color);
  border-left: 3px solid var(--pico-muted-border-color);
  border-radius: 0;
  background: var(--pico-card-background-color);
  color: inherit;
  font-size: 0.72rem;
  text-align: left;
}
.option:hover {
  border-color: var(--pico-primary);
}
.option.selected {
  border-color: #22c55e;
  border-left-color: #22c55e;
  background: color-mix(
    in srgb,
    #22c55e 13%,
    var(--pico-card-background-color)
  );
  box-shadow: 0 0 0 1px #22c55e;
}
.option i {
  color: #22c55e;
}
.modal-list p {
  grid-column: 1 / -1;
  color: var(--pico-muted-color);
  text-align: center;
}
footer {
  justify-content: flex-end;
  padding-top: 1rem;
}
footer button {
  width: 110px;
  margin: 0;
  padding: 0.5rem;
  font-size: 0.72rem;
  font-weight: 700;
}
.custom-scrollbar::-webkit-scrollbar {
  width: 4px;
}
.custom-scrollbar::-webkit-scrollbar-thumb {
  background: var(--pico-muted-border-color);
  border-radius: 10px;
}
.filter-modal-enter-active,
.filter-modal-leave-active {
  transition: opacity 0.16s ease;
}
.filter-modal-enter-active .filter-modal,
.filter-modal-leave-active .filter-modal {
  transition: transform 0.16s ease;
}
.filter-modal-enter-from,
.filter-modal-leave-to {
  opacity: 0;
}
.filter-modal-enter-from .filter-modal,
.filter-modal-leave-to .filter-modal {
  transform: translateY(8px) scale(0.98);
}
@media (max-width: 520px) {
  .modal-list {
    grid-template-columns: 1fr;
  }
}
</style>
