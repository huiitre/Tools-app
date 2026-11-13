<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { useAutofocusMapping } from '../../Dofus/switcher/composables/useAutofocusMapping';

//* Les personnages sont appris automatiquement par l'autofocus (changement de map, connexion) :
//* la saisie manuelle ne sert qu'à corriger ou anticiper.
const { mapping, loadMapping, saveEntry, deleteEntry, clearMapping } = useAutofocusMapping();

const newId = ref('');
const newName = ref('');

const entries = computed(() =>
    Object.entries(mapping.value)
        .map(([id, name]) => ({ id, name }))
        .sort((a, b) => a.name.localeCompare(b.name))
);

const canAdd = computed(() => newId.value.trim() !== '' && newName.value.trim() !== '');

async function addEntry() {
    if (!canAdd.value) return;
    await saveEntry(newId.value.trim().toLowerCase(), newName.value.trim());
    newId.value = '';
    newName.value = '';
}

async function handleClear() {
    if (confirm('Vider tout le mapping ? L\'autofocus réapprendra les personnages au prochain changement de map.')) {
        await clearMapping();
    }
}

onMounted(loadMapping);
</script>

<template>
    <section class="autofocus-mapping">
        <header class="section-header">
            <h3>Mapping Autofocus</h3>
            <p class="section-subtitle">
                Associe l'identifiant réseau de chaque personnage Dofus 3 à son nom de fenêtre.
                Rempli automatiquement en jeu.
            </p>
        </header>

        <div class="card">
            <div class="card-head">
                <span class="card-title">Personnages</span>
                <span class="count">{{ entries.length }}</span>
            </div>

            <ul v-if="entries.length" class="entries">
                <li v-for="entry in entries" :key="entry.id" class="entry">
                    <span class="entry-name">{{ entry.name }}</span>
                    <code class="entry-id">{{ entry.id }}</code>
                    <button
                        class="icon-btn"
                        type="button"
                        :aria-label="`Supprimer ${entry.name}`"
                        title="Supprimer"
                        @click="deleteEntry(entry.id)"
                    >
                        <span class="mdi mdi-close" />
                    </button>
                </li>
            </ul>
            <p v-else class="empty">
                Aucun personnage. Activez l'autofocus et changez de map en jeu pour les détecter.
            </p>

            <form class="add-row" @submit.prevent="addEntry">
                <input v-model="newName" type="text" placeholder="Nom du personnage" aria-label="Nom du personnage" />
                <input v-model="newId" type="text" placeholder="ID (e082…)" aria-label="ID réseau" class="id-input" />
                <button type="submit" class="add-btn" :disabled="!canAdd">Ajouter</button>
            </form>
        </div>

        <div v-if="entries.length" class="footer-actions">
            <button type="button" class="clear-btn" @click="handleClear">Vider le mapping</button>
        </div>
    </section>
</template>

<style scoped>
.section-header {
    margin-bottom: 1rem;
}

.section-subtitle {
    margin: 0.25rem 0 0;
    opacity: 0.75;
    font-size: 0.9rem;
}

.card {
    border: 1px solid var(--pico-muted-border-color);
    border-radius: 10px;
    padding: 1rem;
    max-width: 640px;
}

.card-head {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    margin-bottom: 0.75rem;
}

.card-title {
    font-weight: 600;
}

.count {
    font-size: 0.75rem;
    padding: 0 0.45rem;
    border-radius: 999px;
    background: var(--pico-card-sectioning-background-color);
    color: var(--pico-muted-color);
}

.entries {
    list-style: none;
    margin: 0 0 0.75rem;
    padding: 0;
    display: flex;
    flex-direction: column;
}

.entry {
    list-style: none;
    display: flex;
    align-items: center;
    gap: 0.75rem;
    margin: 0;
    padding: 0.35rem 0.5rem;
    border-radius: 6px;
    font-size: 0.9rem;
}

.entry:hover {
    background: var(--pico-card-sectioning-background-color);
}

.entry-name {
    flex: 1;
    font-weight: 600;
    min-width: 0;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
}

.entry-id {
    font-size: 0.75rem;
    padding: 0.1rem 0.4rem;
    color: var(--pico-muted-color);
}

.icon-btn {
    margin: 0;
    padding: 0.1rem 0.3rem;
    width: auto;
    border: none;
    background: transparent;
    color: var(--pico-muted-color);
    line-height: 1;
}

.icon-btn:hover {
    color: var(--pico-del-color);
}

.empty {
    margin: 0 0 0.75rem;
    font-size: 0.85rem;
    color: var(--pico-muted-color);
}

.add-row {
    display: flex;
    gap: 0.5rem;
    margin: 0;
    padding-top: 0.75rem;
    border-top: 1px solid var(--pico-muted-border-color);
}

.add-row input {
    margin: 0;
    height: 2rem;
    padding: 0 0.6rem;
    font-size: 0.85rem;
    flex: 1;
    min-width: 0;
}

.add-row .id-input {
    flex: 0 0 9rem;
    font-family: var(--pico-font-family-monospace);
}

.add-btn {
    margin: 0;
    height: 2rem;
    padding: 0 0.9rem;
    width: auto;
    font-size: 0.85rem;
}

.footer-actions {
    max-width: 640px;
    display: flex;
    justify-content: flex-end;
    margin-top: 0.75rem;
}

.clear-btn {
    margin: 0;
    width: auto;
    padding: 0.25rem 0.75rem;
    font-size: 0.8rem;
    background: transparent;
    border: 1px solid var(--pico-muted-border-color);
    color: var(--pico-muted-color);
}

.clear-btn:hover {
    border-color: var(--pico-del-color);
    color: var(--pico-del-color);
}

@media (max-width: 560px) {
    .add-row {
        flex-wrap: wrap;
    }

    .add-row .id-input {
        flex: 1 1 100%;
    }
}
</style>
