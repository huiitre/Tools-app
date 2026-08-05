import { computed, onScopeDispose, ref, shallowRef } from 'vue'

export interface WorkerTaskProgress {
  stage: string
  details?: Record<string, number | string>
}

interface WorkerProgressMessage {
  type: 'progress'
  requestId: number
  progress: WorkerTaskProgress
}

interface WorkerResultMessage<TResult> {
  type: 'result'
  requestId: number
  result: TResult
}

interface WorkerErrorMessage {
  type: 'error'
  requestId: number
  message: string
}

type WorkerMessage<TResult> = WorkerProgressMessage | WorkerResultMessage<TResult> | WorkerErrorMessage

export function useWorkerTask<TInput, TResult>(workerFactory: () => Worker) {
  const worker = shallowRef<Worker | null>(null)
  const isRunning = ref(false)
  const startedAt = ref<number | null>(null)
  const now = ref(Date.now())
  const progress = ref<WorkerTaskProgress | null>(null)
  let requestId = 0
  let timer: number | null = null
  let resolveCurrent: ((result: TResult | null) => void) | null = null
  let rejectCurrent: ((reason?: unknown) => void) | null = null

  const elapsedMs = computed(() => startedAt.value === null ? 0 : now.value - startedAt.value)

  function stopTimer() {
    if (timer === null) return
    window.clearInterval(timer)
    timer = null
  }

  function clearTask(result: TResult | null) {
    worker.value?.terminate()
    worker.value = null
    stopTimer()
    isRunning.value = false
    startedAt.value = null
    progress.value = null
    resolveCurrent?.(result)
    resolveCurrent = null
    rejectCurrent = null
  }

  function failTask(message: string) {
    worker.value?.terminate()
    worker.value = null
    stopTimer()
    isRunning.value = false
    startedAt.value = null
    progress.value = null
    rejectCurrent?.(new Error(message))
    resolveCurrent = null
    rejectCurrent = null
  }

  function cancel() {
    if (!isRunning.value) return
    requestId += 1
    clearTask(null)
  }

  function run(input: TInput): Promise<TResult | null> {
    cancel()
    const currentRequestId = ++requestId
    const currentWorker = workerFactory()
    worker.value = currentWorker
    isRunning.value = true
    startedAt.value = Date.now()
    now.value = startedAt.value
    progress.value = { stage: 'Préparation du traitement…' }
    timer = window.setInterval(() => { now.value = Date.now() }, 100)

    return new Promise((resolve, reject) => {
      resolveCurrent = resolve
      rejectCurrent = reject
      currentWorker.onmessage = (event: MessageEvent<WorkerMessage<TResult>>) => {
        const message = event.data
        if (message.requestId !== currentRequestId || worker.value !== currentWorker) return

        if (message.type === 'progress') {
          progress.value = message.progress
          return
        }
        if (message.type === 'result') {
          clearTask(message.result)
          return
        }
        failTask(message.message)
      }
      currentWorker.onerror = () => {
        if (worker.value === currentWorker) failTask('Le traitement dans le Worker a échoué.')
      }
      try {
        currentWorker.postMessage({ type: 'start', requestId: currentRequestId, input })
      } catch {
        failTask('Les données du traitement ne peuvent pas être transmises au Worker.')
      }
    })
  }

  onScopeDispose(cancel)

  return { isRunning, progress, elapsedMs, run, cancel }
}
