<script setup lang="ts">
import { ref } from 'vue'
import type { CodenameWord, CodenameTag, CodenameProposal } from '@/modules/Codename/codename.types'

type AdminTab = 'mots' | 'tags' | 'propositions' | 'parties'

const activeTab = ref<AdminTab>('parties')

// ── Mock data ──────────────────────────────────────────────────────────────────

const mockTags: CodenameTag[] = [
  { id: '1', label: 'Anime' },
  { id: '2', label: 'Jeux Vidéo' },
  { id: '3', label: 'Cinéma' },
  { id: '4', label: 'Sport' },
  { id: '5', label: 'Nature' },
]

const mockWords: CodenameWord[] = [
  { id: '1', content: 'Naruto', validated: true, createdAt: '2026-05-01', tags: [mockTags[0]] },
  { id: '2', content: 'Pikachu', validated: true, createdAt: '2026-05-02', tags: [mockTags[0], mockTags[1]] },
  { id: '3', content: 'Matrix', validated: true, createdAt: '2026-05-03', tags: [mockTags[2]] },
  { id: '4', content: 'Football', validated: false, createdAt: '2026-05-04', tags: [mockTags[3]] },
  { id: '5', content: 'Forêt', validated: true, createdAt: '2026-05-05', tags: [mockTags[4]] },
]

const mockProposals: CodenameProposal[] = [
  { id: '1', content: 'Zelda', suggestedTags: ['Jeux Vidéo'], proposedBy: 'Huiitre', status: 'PENDING', createdAt: '2026-05-18' },
  { id: '2', content: 'Dune', suggestedTags: ['Cinéma'], proposedBy: 'Alice', status: 'PENDING', createdAt: '2026-05-17' },
  { id: '3', content: 'Minecraft', suggestedTags: ['Jeux Vidéo'], proposedBy: null, status: 'PENDING', createdAt: '2026-05-16' },
]

// ── Mots ──────────────────────────────────────────────────────────────────────

const wordSearch = ref('')
const newWord = ref('')
const selectedTagIds = ref<string[]>([])

// ── Tags ──────────────────────────────────────────────────────────────────────

const newTag = ref('')

// ── Créer une partie ──────────────────────────────────────────────────────────

const selectedTagsForGame = ref<string[]>([])
const createdGameUrl = ref<string | null>(null)

function createGame() {
  const fakeUuid = crypto.randomUUID()
  createdGameUrl.value = `${window.location.origin}/codename/game/${fakeUuid}`
}

function copyUrl() {
  if (createdGameUrl.value) {
    navigator.clipboard.writeText(createdGameUrl.value)
  }
}
</script>

<template>
  <div class="codename-admin">
    <div class="admin-tabs">
      <button
        v-for="tab in (['parties', 'mots', 'tags', 'propositions'] as AdminTab[])"
        :key="tab"
        class="tab-btn"
        :class="{ active: activeTab === tab }"
        @click="activeTab = tab"
      >
        {{ tab === 'parties' ? 'Créer une partie' : tab === 'mots' ? 'Mots' : tab === 'tags' ? 'Tags' : 'Propositions' }}
        <span v-if="tab === 'propositions'" class="badge">{{ mockProposals.length }}</span>
      </button>
    </div>

    <!-- ── Créer une partie ── -->
    <section v-if="activeTab === 'parties'" class="admin-section">
      <h2>Créer une nouvelle partie</h2>
      <p class="hint">Sélectionnez des tags pour filtrer le dictionnaire (optionnel). Sans sélection, tous les mots validés sont utilisés.</p>

      <div class="tag-filter">
        <label>Filtrer par tags</label>
        <div class="tag-list">
          <label
            v-for="tag in mockTags"
            :key="tag.id"
            class="tag-checkbox"
          >
            <input type="checkbox" :value="tag.id" v-model="selectedTagsForGame" />
            {{ tag.label }}
          </label>
        </div>
      </div>

      <button class="create-btn" @click="createGame">Créer la partie</button>

      <div v-if="createdGameUrl" class="game-url-box">
        <p>Partie créée ! Partagez ce lien :</p>
        <div class="url-row">
          <code>{{ createdGameUrl }}</code>
          <button @click="copyUrl">Copier</button>
        </div>
      </div>
    </section>

    <!-- ── Mots ── -->
    <section v-if="activeTab === 'mots'" class="admin-section">
      <div class="section-header">
        <h2>Dictionnaire <span class="count">({{ mockWords.length }} mots)</span></h2>
        <input v-model="wordSearch" placeholder="Rechercher un mot…" class="search-input" />
      </div>

      <div class="add-word-row">
        <input v-model="newWord" placeholder="Nouveau mot…" />
        <div class="tag-select">
          <label v-for="tag in mockTags" :key="tag.id" class="tag-checkbox">
            <input type="checkbox" :value="tag.id" v-model="selectedTagIds" />
            {{ tag.label }}
          </label>
        </div>
        <button class="create-btn small">Ajouter</button>
      </div>

      <table class="words-table">
        <thead>
          <tr>
            <th>Mot</th>
            <th>Tags</th>
            <th>Actif</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          <tr
            v-for="word in mockWords.filter(w => w.content.toLowerCase().includes(wordSearch.toLowerCase()))"
            :key="word.id"
          >
            <td>{{ word.content }}</td>
            <td>
              <span v-for="tag in word.tags" :key="tag.id" class="tag-pill">{{ tag.label }}</span>
            </td>
            <td>
              <span class="status-dot" :class="word.validated ? 'active' : 'inactive'" />
            </td>
            <td>
              <button class="action-btn danger">Supprimer</button>
            </td>
          </tr>
        </tbody>
      </table>
    </section>

    <!-- ── Tags ── -->
    <section v-if="activeTab === 'tags'" class="admin-section">
      <div class="section-header">
        <h2>Tags <span class="count">({{ mockTags.length }})</span></h2>
      </div>

      <div class="add-word-row">
        <input v-model="newTag" placeholder="Nouveau tag…" />
        <button class="create-btn small">Ajouter</button>
      </div>

      <ul class="tag-full-list">
        <li v-for="tag in mockTags" :key="tag.id">
          <span class="tag-pill">{{ tag.label }}</span>
          <button class="action-btn danger">Supprimer</button>
        </li>
      </ul>
    </section>

    <!-- ── Propositions ── -->
    <section v-if="activeTab === 'propositions'" class="admin-section">
      <h2>Propositions en attente <span class="count">({{ mockProposals.length }})</span></h2>

      <div class="proposals-list">
        <article v-for="proposal in mockProposals" :key="proposal.id" class="proposal-card">
          <div class="proposal-info">
            <strong>{{ proposal.content }}</strong>
            <span class="hint">par {{ proposal.proposedBy ?? 'Invité' }}</span>
            <div class="proposal-tags">
              <span
                v-for="tag in proposal.suggestedTags"
                :key="tag"
                class="tag-pill"
              >{{ tag }}</span>
            </div>
          </div>
          <div class="proposal-actions">
            <button class="action-btn success">Valider</button>
            <button class="action-btn danger">Rejeter</button>
          </div>
        </article>
      </div>
    </section>
  </div>
</template>

<style lang="scss" scoped>
.codename-admin {
  max-width: 900px;
  margin: 0 auto;
  padding: 1.5rem 1rem;
}

.admin-tabs {
  display: flex;
  gap: 0.5rem;
  border-bottom: 1px solid var(--pico-muted-border-color);
  margin-bottom: 1.5rem;
}

.tab-btn {
  background: none;
  border: none;
  border-bottom: 2px solid transparent;
  margin-bottom: -1px;
  padding: 0.5rem 1rem;
  cursor: pointer;
  font-size: 0.85rem;
  letter-spacing: 0.05em;
  text-transform: uppercase;
  color: var(--pico-muted-color);
  transition: color 0.2s, border-color 0.2s;
  display: flex;
  align-items: center;
  gap: 0.4rem;

  &.active {
    color: var(--pico-primary);
    border-bottom-color: var(--pico-primary);
  }

  &:hover:not(.active) {
    color: var(--pico-color);
  }
}

.badge {
  background: var(--pico-primary);
  color: var(--pico-primary-inverse);
  border-radius: 999px;
  font-size: 0.7rem;
  padding: 0.1rem 0.45rem;
  font-weight: 600;
}

.admin-section {
  h2 {
    margin-bottom: 0.75rem;
    font-size: 1.1rem;
  }
}

.section-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 1rem;
}

.count {
  font-size: 0.85rem;
  color: var(--pico-muted-color);
  font-weight: normal;
}

.hint {
  font-size: 0.85rem;
  color: var(--pico-muted-color);
  margin-bottom: 1rem;
}

.tag-filter {
  margin-bottom: 1.5rem;

  label:first-child {
    display: block;
    font-size: 0.85rem;
    color: var(--pico-muted-color);
    margin-bottom: 0.5rem;
  }
}

.tag-list {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
}

.tag-checkbox {
  display: flex;
  align-items: center;
  gap: 0.3rem;
  font-size: 0.85rem;
  cursor: pointer;

  input[type="checkbox"] {
    margin: 0;
  }
}

.create-btn {
  background: var(--pico-primary);
  color: var(--pico-primary-inverse);
  border: none;
  border-radius: var(--pico-border-radius);
  padding: 0.6rem 1.25rem;
  cursor: pointer;
  font-size: 0.9rem;
  transition: opacity 0.2s;

  &:hover { opacity: 0.85; }

  &.small {
    padding: 0.4rem 0.9rem;
    font-size: 0.8rem;
  }
}

.game-url-box {
  margin-top: 1.5rem;
  padding: 1rem;
  border: 1px solid var(--pico-muted-border-color);
  border-radius: var(--pico-border-radius);
  background: var(--pico-card-background-color);

  p { margin-bottom: 0.5rem; font-size: 0.9rem; }
}

.url-row {
  display: flex;
  align-items: center;
  gap: 0.75rem;

  code {
    flex: 1;
    font-size: 0.8rem;
    word-break: break-all;
    color: var(--pico-muted-color);
  }

  button {
    padding: 0.35rem 0.75rem;
    font-size: 0.8rem;
    border: 1px solid var(--pico-muted-border-color);
    background: transparent;
    border-radius: var(--pico-border-radius);
    cursor: pointer;
    color: var(--pico-color);
  }
}

.add-word-row {
  display: flex;
  gap: 0.75rem;
  align-items: center;
  margin-bottom: 1rem;
  flex-wrap: wrap;

  input {
    flex: 0 0 200px;
    padding: 0.4rem 0.6rem;
    font-size: 0.9rem;
    border: 1px solid var(--pico-muted-border-color);
    border-radius: var(--pico-border-radius);
    background: var(--pico-background-color);
    color: var(--pico-color);
  }

  .tag-select {
    display: flex;
    gap: 0.5rem;
    flex-wrap: wrap;
  }
}

.search-input {
  padding: 0.4rem 0.6rem;
  font-size: 0.85rem;
  border: 1px solid var(--pico-muted-border-color);
  border-radius: var(--pico-border-radius);
  background: var(--pico-background-color);
  color: var(--pico-color);
  width: 200px;
}

.words-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 0.88rem;

  th {
    text-align: left;
    padding: 0.5rem 0.75rem;
    border-bottom: 1px solid var(--pico-muted-border-color);
    color: var(--pico-muted-color);
    font-weight: 600;
    font-size: 0.78rem;
    text-transform: uppercase;
    letter-spacing: 0.05em;
  }

  td {
    padding: 0.6rem 0.75rem;
    border-bottom: 1px solid var(--pico-muted-border-color);
    vertical-align: middle;
  }
}

.tag-pill {
  display: inline-block;
  font-size: 0.72rem;
  padding: 0.15rem 0.5rem;
  border-radius: 999px;
  background: var(--pico-secondary-background);
  color: var(--pico-secondary-foreground, var(--pico-color));
  border: 1px solid var(--pico-muted-border-color);
  margin-right: 0.25rem;
}

.status-dot {
  display: inline-block;
  width: 8px;
  height: 8px;
  border-radius: 50%;

  &.active { background: #22c55e; }
  &.inactive { background: var(--pico-muted-color); }
}

.action-btn {
  font-size: 0.75rem;
  padding: 0.25rem 0.6rem;
  border-radius: var(--pico-border-radius);
  border: 1px solid transparent;
  cursor: pointer;
  transition: opacity 0.2s;

  &.danger {
    background: transparent;
    border-color: #ef4444;
    color: #ef4444;
  }

  &.success {
    background: transparent;
    border-color: #22c55e;
    color: #22c55e;
  }

  &:hover { opacity: 0.75; }
}

.tag-full-list {
  list-style: none;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 0.5rem;

  li {
    display: flex;
    align-items: center;
    gap: 0.75rem;
  }
}

.proposals-list {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.proposal-card {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0.875rem 1rem;
  border: 1px solid var(--pico-muted-border-color);
  border-radius: var(--pico-border-radius);
  background: var(--pico-card-background-color);
}

.proposal-info {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;

  strong { font-size: 0.95rem; }

  .hint {
    margin: 0;
    font-size: 0.78rem;
  }
}

.proposal-tags {
  display: flex;
  gap: 0.25rem;
  flex-wrap: wrap;
  margin-top: 0.1rem;
}

.proposal-actions {
  display: flex;
  gap: 0.5rem;
}
</style>