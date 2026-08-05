<script setup lang="ts">
const props = withDefaults(defineProps<{
  open: boolean
  title: string
  description?: string
  stage: string
  elapsedMs: number
  details?: { label: string; value: string | number }[]
}>(), {
  description: '',
  details: () => [],
})

const emit = defineEmits<{
  cancel: []
}>()

function formatElapsedTime(elapsedMs: number) {
  const seconds = elapsedMs / 1000
  return seconds < 10 ? `${seconds.toFixed(1)} s` : `${Math.floor(seconds)} s`
}
</script>

<template>
  <Teleport to="body">
    <Transition name="worker-task-modal">
      <div v-if="props.open" class="worker-task-modal-backdrop">
        <section class="worker-task-modal" role="dialog" aria-modal="true" :aria-label="props.title">
          <div class="worker-task-modal__spinner" aria-hidden="true"><i class="mdi mdi-cog-outline" /></div>
          <strong>{{ props.title }}</strong>
          <small v-if="props.description">{{ props.description }}</small>

          <div class="worker-task-modal__status">
            <span>{{ props.stage }}</span>
            <time>{{ formatElapsedTime(props.elapsedMs) }}</time>
          </div>
          <div class="worker-task-modal__bar"><span /></div>

          <dl v-if="props.details.length" class="worker-task-modal__details">
            <div v-for="detail in props.details" :key="detail.label">
              <dt>{{ detail.label }}</dt>
              <dd>{{ detail.value }}</dd>
            </div>
          </dl>

          <button type="button" class="secondary outline" @click="emit('cancel')">
            <i class="mdi mdi-close-circle-outline" /> Annuler
          </button>
        </section>
      </div>
    </Transition>
  </Teleport>
</template>

<style scoped lang="scss">
.worker-task-modal-backdrop { position: fixed; z-index: 1200; inset: 0; display: grid; place-items: center; padding: 1rem; background: rgb(0 0 0 / 67%); backdrop-filter: blur(5px) saturate(.75); }
.worker-task-modal { display: flex; flex-direction: column; align-items: stretch; width: min(390px, 100%); margin: 0; padding: 1.25rem; border: 1px solid var(--pico-muted-border-color); border-radius: 14px; background: color-mix(in srgb, var(--pico-card-background-color) 92%, #000); box-shadow: 0 20px 65px rgb(0 0 0 / 55%); text-align: center; }
.worker-task-modal__spinner { display: grid; place-items: center; width: 2.7rem; height: 2.7rem; margin: 0 auto .65rem; border: 1px solid color-mix(in srgb, var(--pico-primary) 55%, transparent); border-radius: 50%; color: var(--pico-primary); font-size: 1.35rem; }
.worker-task-modal__spinner i { animation: worker-spin 1.1s linear infinite; }
.worker-task-modal > strong { font-size: .92rem; }
.worker-task-modal > small { margin-top: .3rem; color: var(--pico-muted-color); font-size: .68rem; }
.worker-task-modal__status { display: flex; justify-content: space-between; gap: 1rem; margin-top: 1.15rem; color: var(--pico-muted-color); font-size: .68rem; text-align: left; }
.worker-task-modal__status time { flex: 0 0 auto; color: var(--pico-color); font-variant-numeric: tabular-nums; }
.worker-task-modal__bar { height: .3rem; margin-top: .45rem; overflow: hidden; border-radius: 999px; background: color-mix(in srgb, var(--pico-muted-color) 20%, transparent); }
.worker-task-modal__bar span { display: block; width: 36%; height: 100%; border-radius: inherit; background: var(--pico-primary); animation: worker-progress 1.35s ease-in-out infinite; }
.worker-task-modal__details { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: .45rem; margin: 1rem 0; text-align: left; }
.worker-task-modal__details div { padding: .5rem; border: 1px solid color-mix(in srgb, var(--pico-muted-border-color) 75%, transparent); border-radius: var(--pico-border-radius); }
.worker-task-modal__details dt { color: var(--pico-muted-color); font-size: .58rem; }
.worker-task-modal__details dd { margin: .15rem 0 0; font-size: .75rem; font-weight: 700; }
.worker-task-modal button { width: auto; align-self: center; margin: 0; padding: .45rem 1rem; font-size: .7rem; }
.worker-task-modal-enter-active, .worker-task-modal-leave-active { transition: opacity .16s ease; }
.worker-task-modal-enter-active .worker-task-modal, .worker-task-modal-leave-active .worker-task-modal { transition: transform .16s ease; }
.worker-task-modal-enter-from, .worker-task-modal-leave-to { opacity: 0; }
.worker-task-modal-enter-from .worker-task-modal, .worker-task-modal-leave-to .worker-task-modal { transform: translateY(8px) scale(.98); }
@keyframes worker-spin { to { transform: rotate(360deg); } }
@keyframes worker-progress { 0% { transform: translateX(-120%); } 55% { transform: translateX(220%); } 100% { transform: translateX(220%); } }
</style>
