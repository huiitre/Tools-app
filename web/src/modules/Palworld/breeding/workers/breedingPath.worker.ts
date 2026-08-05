/// <reference lib="webworker" />

import {
  BreedingPathEngine,
} from '../services/BreedingPathEngine'
import type { BreedingPathWorkerInput } from './breedingPathWorker.types'

interface StartMessage {
  type: 'start'
  requestId: number
  input: BreedingPathWorkerInput
}

const worker = self as DedicatedWorkerGlobalScope

worker.onmessage = (event: MessageEvent<StartMessage>) => {
  const { type, requestId, input } = event.data
  if (type !== 'start') return

  try {
    worker.postMessage({
      type: 'progress',
      requestId,
      progress: { stage: 'Préparation des combinaisons…', details: { 'Pals analysés': input.ownedPals.length } },
    })
    const engine = new BreedingPathEngine(input.pals, input.rules)
    worker.postMessage({
      type: 'progress',
      requestId,
      progress: { stage: 'Combinaisons prêtes, recherche des routes directes…', details: { 'Pals analysés': input.ownedPals.length } },
    })
    const result = engine.compute(
      input.targetId,
      input.ownedPals,
      input.passiveSkillIds,
      input.options,
      progress => worker.postMessage({
        type: 'progress',
        requestId,
        progress: {
          stage: progress.stage === 'exploring' ? 'Recherche des chemins d’élevage…' : 'Construction des meilleures routes…',
          details: {
            'États explorés': progress.exploredStates,
            'Routes candidates': progress.candidateRoutes,
          },
        },
      }),
    )
    worker.postMessage({ type: 'result', requestId, result })
  } catch {
    worker.postMessage({ type: 'error', requestId, message: 'Impossible de calculer le chemin.' })
  }
}
