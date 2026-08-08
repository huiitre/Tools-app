import { defineConfig } from 'vitest/config'
import { resolve } from 'path'

// Config Vitest séparée de vite.config.ts pour ne pas embarquer les plugins Vue/PWA
// (inutiles pour des tests de logique pure) dans le runner de tests.
export default defineConfig({
  resolve: {
    alias: {
      '@': resolve(__dirname, 'src'),
    },
  },
  test: {
    include: ['src/**/*.spec.ts'],
    environment: 'node',
  },
})
