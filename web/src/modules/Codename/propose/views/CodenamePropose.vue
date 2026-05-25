<script setup lang="ts">
import { ref } from 'vue'
import type { CodenameTag } from '@/modules/Codename/codename.types'

const word = ref('')
const selectedTags = ref<string[]>([])
const submitted = ref(false)
const error = ref('')

const mockTags: CodenameTag[] = [
  { id: '1', label: 'Anime' },
  { id: '2', label: 'Jeux Vidéo' },
  { id: '3', label: 'Cinéma' },
  { id: '4', label: 'Sport' },
  { id: '5', label: 'Nature' },
  { id: '6', label: 'Musique' },
  { id: '7', label: 'Histoire' },
]

function submit() {
  error.value = ''

  if (!word.value.trim()) {
    error.value = 'Le mot est requis.'
    return
  }

  if (word.value.trim().length > 50) {
    error.value = 'Le mot ne doit pas dépasser 50 caractères.'
    return
  }

  submitted.value = true
}

function reset() {
  word.value = ''
  selectedTags.value = []
  submitted.value = false
  error.value = ''
}
</script>

<template>
  <div class="codename-propose">
    <div class="propose-card">
      <h2>Proposer un mot</h2>
      <p class="hint">
        Les propositions sont examinées par les modérateurs avant d'être ajoutées au dictionnaire.
      </p>

      <template v-if="!submitted">
        <div class="field">
          <label>Mot <span class="required">*</span></label>
          <input
            v-model="word"
            placeholder="Ex : Pikachu"
            maxlength="50"
            :class="{ invalid: error }"
            @keydown.enter="submit"
          />
          <span v-if="error" class="error-msg">{{ error }}</span>
        </div>

        <div class="field">
          <label>Tags suggérés <span class="optional">(optionnel)</span></label>
          <div class="tag-grid">
            <label
              v-for="tag in mockTags"
              :key="tag.id"
              class="tag-option"
              :class="{ selected: selectedTags.includes(tag.id) }"
            >
              <input type="checkbox" :value="tag.id" v-model="selectedTags" />
              {{ tag.label }}
            </label>
          </div>
        </div>

        <button class="submit-btn" @click="submit">Envoyer la proposition</button>
      </template>

      <div v-else class="success-state">
        <div class="success-icon">✓</div>
        <h3>Proposition envoyée !</h3>
        <p>Merci ! Le mot <strong>« {{ word }} »</strong> sera examiné par un modérateur.</p>
        <button class="secondary-btn" @click="reset">Proposer un autre mot</button>
      </div>
    </div>
  </div>
</template>

<style lang="scss" scoped>
.codename-propose {
  max-width: 520px;
  margin: 2rem auto;
  padding: 0 1rem;
}

.propose-card {
  padding: 2rem;
  border: 1px solid var(--pico-muted-border-color);
  border-radius: var(--pico-border-radius);
  background: var(--pico-card-background-color);

  h2 {
    margin-top: 0;
    margin-bottom: 0.25rem;
    font-size: 1.15rem;
  }
}

.hint {
  font-size: 0.85rem;
  color: var(--pico-muted-color);
  margin-bottom: 1.5rem;
}

.field {
  margin-bottom: 1.25rem;

  label {
    display: block;
    font-size: 0.85rem;
    margin-bottom: 0.4rem;
    font-weight: 500;
  }

  input[type="text"], input:not([type="checkbox"]) {
    width: 100%;
    padding: 0.5rem 0.75rem;
    font-size: 0.9rem;
    border: 1px solid var(--pico-muted-border-color);
    border-radius: var(--pico-border-radius);
    background: var(--pico-background-color);
    color: var(--pico-color);
    box-sizing: border-box;

    &.invalid {
      border-color: #ef4444;
    }
  }
}

.required { color: #ef4444; }
.optional { color: var(--pico-muted-color); font-weight: normal; font-size: 0.78rem; }

.error-msg {
  display: block;
  font-size: 0.78rem;
  color: #ef4444;
  margin-top: 0.3rem;
}

.tag-grid {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
}

.tag-option {
  display: flex;
  align-items: center;
  gap: 0.3rem;
  font-size: 0.82rem;
  padding: 0.3rem 0.7rem;
  border: 1px solid var(--pico-muted-border-color);
  border-radius: 999px;
  cursor: pointer;
  transition: border-color 0.2s, background 0.2s;
  user-select: none;

  input { display: none; }

  &.selected {
    border-color: var(--pico-primary);
    background: rgba(var(--pico-primary-rgb, 99, 102, 241), 0.1);
    color: var(--pico-primary);
  }

  &:hover:not(.selected) {
    border-color: var(--pico-color);
  }
}

.submit-btn {
  width: 100%;
  background: var(--pico-primary);
  color: var(--pico-primary-inverse);
  border: none;
  border-radius: var(--pico-border-radius);
  padding: 0.65rem;
  font-size: 0.9rem;
  cursor: pointer;
  transition: opacity 0.2s;

  &:hover { opacity: 0.85; }
}

.success-state {
  text-align: center;
  padding: 1rem 0;

  .success-icon {
    width: 48px;
    height: 48px;
    border-radius: 50%;
    background: rgba(34, 197, 94, 0.15);
    color: #22c55e;
    font-size: 1.4rem;
    display: flex;
    align-items: center;
    justify-content: center;
    margin: 0 auto 1rem;
  }

  h3 { margin-bottom: 0.5rem; }

  p {
    color: var(--pico-muted-color);
    font-size: 0.9rem;
    margin-bottom: 1.5rem;
  }
}

.secondary-btn {
  background: transparent;
  border: 1px solid var(--pico-muted-border-color);
  border-radius: var(--pico-border-radius);
  padding: 0.5rem 1.25rem;
  font-size: 0.88rem;
  cursor: pointer;
  color: var(--pico-color);
  transition: border-color 0.2s;

  &:hover { border-color: var(--pico-primary); }
}
</style>