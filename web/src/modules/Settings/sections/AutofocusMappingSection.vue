<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { useAutofocusMapping } from '../../Dofus/switcher/composables/useAutofocusMapping';

const { mapping, loadMapping, saveEntry, deleteEntry, clearMapping } = useAutofocusMapping();

const newId = ref('');
const newName = ref('');

async function addEntry() {
    if (newId.value && newName.value) {
        await saveEntry(newId.value.trim(), newName.value.trim());
        newId.value = '';
        newName.value = '';
    }
}

async function handleClear() {
    if (confirm('Voulez-vous vraiment vider tout le mapping ? Cela forcera l\'Autofocus à ré-apprendre les personnages.')) {
        await clearMapping();
    }
}

onMounted(async () => {
    await loadMapping();
    
    // Écouter les mises à jour automatiques depuis Electron
    window.electron?.onAutofocusMappingUpdated(async (newMapping: Record<string, string>) => {
        for (const [id, name] of Object.entries(newMapping)) {
            if (mapping.value[id] !== name) {
                await saveEntry(id, name);
            }
        }
    });
});
</script>

<template>
    <section id="autofocus-mapping">
        <h3>Mapping Autofocus</h3>
        <p>Le mapping associe l'ID interne de Dofus au nom du personnage pour le focus automatique.</p>

        <div class="grid">
            <input v-model="newId" type="text" placeholder="ID Hex (ex: e082...)" />
            <input v-model="newName" type="text" placeholder="Nom du personnage" />
            <button class="outline" @click="addEntry">Ajouter</button>
        </div>

        <table role="grid">
            <thead>
                <tr>
                    <th>ID Hex</th>
                    <th>Personnage</th>
                    <th style="width: 50px;"></th>
                </tr>
            </thead>
            <tbody>
                <tr v-for="(name, id) in mapping" :key="id">
                    <td><code>{{ id }}</code></td>
                    <td>{{ name }}</td>
                    <td>
                        <a href="#" class="del-btn" @click.prevent="deleteEntry(id)">
                            <span class="mdi mdi-delete" />
                        </a>
                    </td>
                </tr>
                <tr v-if="Object.keys(mapping).length === 0">
                    <td colspan="3" style="text-align: center; color: var(--secondary);">
                        Aucun mapping détecté. Jouez en jeu pour remplir automatiquement.
                    </td>
                </tr>
            </tbody>
        </table>

        <div class="footer-actions">
            <button class="secondary outline" @click="handleClear">Vider le cache</button>
        </div>
    </section>
</template>

<style scoped>
.del-btn {
    color: var(--del-color);
}
.del-btn:hover {
    color: var(--primary);
}
.footer-actions {
    display: flex;
    justify-content: flex-end;
    margin-top: 1rem;
}
code {
    font-size: 0.85rem;
}
</style>
