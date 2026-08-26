<script setup lang="ts">
import { onMounted } from 'vue'
import { useAdminFeedbacksStore } from '../store/adminFeedbacks.store'
import { fetchAdminFeedbacks } from '@/modules/Core/Feedback/fetch/feedback.fetch'
import AdminFeedbacksToolbar from '../components/AdminFeedbacksToolbar.vue'
import AdminFeedbacksHeader from '../components/AdminFeedbacksHeader.vue'
import AdminFeedbacksRow from '../components/AdminFeedbacksRow.vue'
import toast from '@/services/toast'

const store = useAdminFeedbacksStore()

onMounted(async () => {
  store.loading = true
  try {
    store.feedbacks = await fetchAdminFeedbacks()
  } catch {
    toast.error('Impossible de charger les feedbacks')
  } finally {
    store.loading = false
  }
})
</script>

<template>
  <main class="admin-feedbacks">
    <AdminFeedbacksToolbar />
    <AdminFeedbacksHeader />

    <template v-if="store.loading">
      <div v-for="i in 10" :key="i" class="skeleton-row" />
    </template>

    <template v-else-if="store.paginated.length">
      <AdminFeedbacksRow v-for="fb in store.paginated" :key="fb.id" :feedback="fb" />
    </template>

    <div v-else class="empty">
      <i class="mdi mdi-message-off-outline" />
      Aucun feedback trouvé
    </div>
  </main>
</template>

<style scoped lang="scss">
.admin-feedbacks {
  padding: 0.75rem;
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
  font-size: 0.85rem;
}

.skeleton-row {
  height: 38px;
  border-radius: 0.45rem;
  background: linear-gradient(
    90deg,
    var(--pico-card-background-color) 0%,
    var(--pico-muted-border-color) 50%,
    var(--pico-card-background-color) 100%
  );
  background-size: 200% 100%;
  animation: shimmer 1.6s ease-in-out infinite;
}

@keyframes shimmer {
  0%   { background-position: 200% 0; }
  100% { background-position: -200% 0; }
}

.empty {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  padding: 3rem;
  color: var(--pico-muted-color);
  font-size: 0.9rem;

  i { font-size: 1.25rem; }
}
</style>
