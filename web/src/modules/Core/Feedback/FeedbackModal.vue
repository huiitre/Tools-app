<script setup lang="ts">
import { ref, computed } from 'vue';
import { feedbackService } from './feedback.service';
import toast from '@/services/toast';

const emit = defineEmits(['close']);

const message = ref('');
const isSubmitting = ref(false);
const MAX_LENGTH = 500;

const charCount = computed(() => message.value.length);
const isValid = computed(() => {
  const trimmed = message.value.trim();
  return trimmed.length > 0 && trimmed.length <= MAX_LENGTH;
});

const close = () => {
  if (isSubmitting.value) return;
  emit('close');
};

const submitFeedback = async () => {
  if (!isValid.value || isSubmitting.value) return;

  isSubmitting.value = true;
  try {
    await feedbackService.create(message.value.trim());
    toast.success('Merci pour votre retour !');
    emit('close');
  } catch (error) {
    toast.error('Erreur lors de l\'envoi du feedback');
    console.error(error);
  } finally {
    isSubmitting.value = false;
  }
};
</script>

<template>
  <Teleport to="body">
    <div class="modal-overlay" @click.self="close">
      <div class="modal">
        <div class="modal-header">
          <div class="modal-title-group">
            <i class="mdi mdi-bug" aria-hidden="true"></i>
            <span class="modal-title">Feedback</span>
          </div>
          <button class="close-btn" :disabled="isSubmitting" aria-label="Fermer" @click="close">
            <i class="mdi mdi-close" aria-hidden="true"></i>
          </button>
        </div>

        <p class="modal-sub">Suggère une amélioration ou signale un bug.</p>

        <textarea
          v-model="message"
          placeholder="Décris ton retour ici…"
          rows="5"
          :disabled="isSubmitting"
          :maxlength="MAX_LENGTH"
          class="feedback-textarea"
        />

        <div class="modal-footer">
          <small :class="['char-count', { 'char-count--over': charCount > MAX_LENGTH }]">
            {{ charCount }} / {{ MAX_LENGTH }}
          </small>
          <div class="modal-actions">
            <button
              class="btn-primary"
              :disabled="!isValid || isSubmitting"
              @click="submitFeedback"
            >
              {{ isSubmitting ? 'Envoi…' : 'Envoyer' }}
            </button>
          </div>
        </div>
      </div>
    </div>
  </Teleport>
</template>

<style scoped lang="scss">
.modal-overlay {
  position: fixed;
  inset: 0;
  z-index: 9000;
  background: rgba(0, 0, 0, 0.6);
  display: flex;
  align-items: center;
  justify-content: center;
}

.modal {
  background: var(--pico-card-background-color);
  border: 1px solid var(--pico-card-border-color);
  border-radius: 0.65rem;
  padding: 1.5rem;
  width: 460px;
  max-width: 90vw;
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  box-shadow: var(--pico-card-box-shadow);
}

.modal-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.modal-title-group {
  display: flex;
  align-items: center;
  gap: 0.5rem;

  i { font-size: 0.9rem; color: var(--pico-primary); }
}

.modal-title {
  font-size: 1rem;
  font-weight: 700;
}

.modal-sub {
  font-size: 0.8rem;
  color: var(--pico-muted-color);
  margin: 0;
}

.close-btn {
  width: 1.75rem;
  height: 1.75rem;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  padding: 0;
  border: none;
  background: transparent;
  color: var(--pico-muted-color);
  cursor: pointer;
  border-radius: 0.35rem;

  &:hover:not(:disabled) { color: var(--pico-color); background: var(--pico-muted-border-color); }
  &:disabled { opacity: 0.5; cursor: not-allowed; }
}

.feedback-textarea {
  resize: none;
  margin: 0;
  font-size: 0.85rem;
}

.modal-footer {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.5rem;
}

.char-count {
  font-size: 0.75rem;
  color: var(--pico-muted-color);

  &--over { color: #ef4444; }
}

.modal-actions {
  display: flex;
  gap: 0.5rem;
}

.btn-primary {
  padding: 0.45rem 1rem;
  background: var(--pico-primary);
  color: var(--pico-primary-inverse);
  border: none;
  border-radius: 0.35rem;
  font-size: 0.85rem;
  cursor: pointer;

  &:disabled { opacity: 0.5; cursor: not-allowed; }
  &:hover:not(:disabled) { opacity: 0.85; }
}

.btn-secondary {
  padding: 0.45rem 1rem;
  background: none;
  color: var(--pico-muted-color);
  border: 1px solid var(--pico-muted-border-color);
  border-radius: 0.35rem;
  font-size: 0.85rem;
  cursor: pointer;

  &:disabled { opacity: 0.5; cursor: not-allowed; }
  &:hover:not(:disabled) { border-color: var(--pico-color); color: var(--pico-color); }
}
</style>
