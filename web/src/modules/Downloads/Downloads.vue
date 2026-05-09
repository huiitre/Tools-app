<script setup lang="ts">
import { useOS } from '@/composables/useOS'
import { useReleases } from '@/modules/Downloads/composables/useReleases'

const { isWindows, isLinux } = useOS()
const { latest, releases, loading, error } = useReleases()
</script>

<template>
  <main class="downloads-page">

    <div v-if="loading" class="state-message">
      <p>Chargement des versions...</p>
    </div>

    <div v-else-if="error" class="state-message">
      <p>Impossible de récupérer les versions depuis GitHub.</p>
    </div>

    <template v-else-if="latest">

      <!-- HERO -->
      <section class="hero">
        <p class="hero-label">Dernière version</p>
        <h1 class="hero-version">
          Tools <span class="version-tag">v{{ latest.version }}</span>
        </h1>
        <p class="hero-subtitle">Application de bureau disponible sur Windows et Linux.</p>

        <div class="hero-actions">
          <a :href="latest.windows ?? '#'" class="btn" :class="isWindows ? 'btn-primary' : 'btn-secondary'" download>
            <i class="fa-brands fa-windows"></i>
            Windows (.exe)
          </a>
          <a :href="latest.linux ?? '#'" class="btn" :class="isLinux ? 'btn-primary' : 'btn-secondary'" download>
            <i class="fa-brands fa-linux"></i>
            Linux (.AppImage)
          </a>
          <a
            :href="`https://github.com/huiitre/Tools-web/releases/tag/v${latest.version}`"
            class="btn btn-ghost"
            target="_blank"
            rel="noopener noreferrer"
          >
            <i class="fa-brands fa-github"></i>
            Notes de version
          </a>
        </div>

        <ul v-if="latest.changelog.length" class="hero-changelog">
          <li v-for="item in latest.changelog" :key="item.text">
            {{ item.text }}
            <a v-if="item.url" :href="item.url" target="_blank" rel="noopener noreferrer" class="commit-link">
              [{{ item.hash }}]
            </a>
          </li>
        </ul>
      </section>

      <!-- VERSIONS PRÉCÉDENTES -->
      <section class="releases">
        <h2 class="releases-title">Versions précédentes</h2>

        <div class="release-list">
          <div v-for="release in releases" :key="release.version" class="release-card">
            <div class="release-header">
              <div class="release-meta">
                <a
                  :href="`https://github.com/huiitre/Tools-web/releases/tag/v${release.version}`"
                  target="_blank"
                  rel="noopener noreferrer"
                  class="release-github"
                >
                  <i class="fa-brands fa-github"></i>
                </a>
                <span class="release-version">v{{ release.version }}</span>
                <span class="release-date">{{ release.date }}</span>
              </div>
              <div class="release-links">
                <a v-if="release.windows" :href="release.windows" class="release-link" download>
                  <i class="fa-brands fa-windows"></i> Windows
                </a>
                <a v-if="release.linux" :href="release.linux" class="release-link" download>
                  <i class="fa-brands fa-linux"></i> Linux
                </a>
              </div>
            </div>

            <ul v-if="release.changelog.length" class="release-changelog">
              <li v-for="item in release.changelog" :key="item.text">
                {{ item.text }}
                <a v-if="item.url" :href="item.url" target="_blank" rel="noopener noreferrer" class="commit-link">
                  [{{ item.hash }}]
                </a>
              </li>
            </ul>
            <p v-else class="release-no-changelog">Aucune note de version disponible.</p>
          </div>
        </div>
      </section>

    </template>

  </main>
</template>

<style scoped>
.downloads-page {
  max-width: 860px;
  margin: 0 auto;
  padding: 3rem 1.5rem;
  display: flex;
  flex-direction: column;
  gap: 4rem;
}

.state-message {
  text-align: center;
  color: var(--pico-muted-color);
  padding: 4rem 0;
}

/* ===== HERO ===== */

.hero {
  text-align: center;
}

.hero-label {
  font-size: 0.75rem;
  text-transform: uppercase;
  letter-spacing: 0.1em;
  color: var(--pico-muted-color);
  margin-bottom: 0.5rem;
}

.hero-version {
  font-size: 2.5rem;
  font-weight: 700;
  margin-bottom: 0.5rem;
}

.version-tag {
  color: var(--pico-primary);
}

.hero-subtitle {
  color: var(--pico-muted-color);
  margin-bottom: 2rem;
}

.hero-actions {
  display: flex;
  justify-content: center;
  gap: 1rem;
  flex-wrap: wrap;
  margin-bottom: 2rem;
}

.btn {
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.65rem 1.4rem;
  border-radius: var(--pico-border-radius);
  font-size: 0.9rem;
  text-decoration: none;
  font-weight: 500;
  transition: opacity 0.2s ease;
}

.btn:hover {
  opacity: 0.85;
}

.btn-primary {
  background: var(--pico-primary-background);
  color: var(--pico-primary-inverse);
  border: none;
}

.btn-secondary {
  background: transparent;
  color: var(--pico-color);
  border: 1px solid var(--pico-muted-border-color);
}

.btn-ghost {
  background: transparent;
  color: var(--pico-muted-color);
  border: 1px solid var(--pico-muted-border-color);
  font-size: 0.82rem;
}

.btn-ghost:hover {
  color: var(--pico-color);
  border-color: var(--pico-color);
  opacity: 1;
}

.hero-changelog {
  display: inline-block;
  text-align: left;
  margin: 0 auto;
  padding-left: 1.25rem;
}

.hero-changelog li {
  font-size: 0.875rem;
  color: var(--pico-muted-color);
  line-height: 1.8;
}

/* ===== RELEASES ===== */

.releases-title {
  font-size: 1.1rem;
  font-weight: 600;
  margin-bottom: 1rem;
  color: var(--pico-muted-color);
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

.release-list {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.release-card {
  border: 1px solid var(--pico-muted-border-color);
  border-radius: var(--pico-border-radius);
  padding: 1rem 1.25rem;
}

.release-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  flex-wrap: wrap;
  gap: 0.5rem;
  margin-bottom: 0.75rem;
}

.release-meta {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.release-github {
  color: var(--pico-muted-color);
  font-size: 1rem;
  text-decoration: none;
}

.release-github:hover {
  color: var(--pico-color);
}

.release-version {
  font-weight: 600;
  font-size: 0.95rem;
}

.release-date {
  font-size: 0.8rem;
  color: var(--pico-muted-color);
}

.release-links {
  display: flex;
  align-items: center;
  gap: 1rem;
}

.release-link {
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
  font-size: 0.85rem;
  color: var(--pico-primary);
  text-decoration: none;
}

.release-link:hover {
  text-decoration: underline;
}

.release-changelog {
  margin: 0;
  padding-left: 1.25rem;
}

.release-changelog li {
  font-size: 0.85rem;
  color: var(--pico-muted-color);
  line-height: 1.7;
}

.commit-link {
  font-size: 0.75rem;
  color: var(--pico-muted-color);
  text-decoration: none;
  font-family: monospace;
}

.commit-link:hover {
  color: var(--pico-primary);
  text-decoration: underline;
}

.release-no-changelog {
  font-size: 0.8rem;
  color: var(--pico-muted-color);
  font-style: italic;
  margin: 0;
}
</style>