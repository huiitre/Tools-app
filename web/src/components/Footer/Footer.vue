<script setup lang="ts">
import { RouterLink } from 'vue-router'

const year = new Date().getFullYear()

const version = __APP_VERSION__

</script>

<template>
  <footer class="app-footer">
    <nav class="footer-nav">
      <!-- GAUCHE -->
      <div class="footer-left">
        <RouterLink to="/">
          <small>© {{ year }} Tools</small>
        </RouterLink>
      </div>

      <!-- CENTRE -->
      <ul class="footer-links">
        <li>
          <RouterLink to="/privacy-policy">Confidentialité</RouterLink>
        </li>
        <li>
          <RouterLink to="/terms-of-service">Conditions</RouterLink>
        </li>
      </ul>

      <!-- DROITE -->
      <div class="footer-right">
        <span class="app-version">{{ version }}</span>

        <button class="icon-button" aria-label="Feedback">
          <i class="fa-solid fa-bug" aria-hidden="true"></i>
        </button>
      </div>
    </nav>
  </footer>
</template>

<style scoped>
.app-footer {
  width: 100%;
  padding: 1rem;
  margin-top: auto;
  border-top: 1px solid var(--pico-muted-border-color);
  color: var(--pico-color);
  background-color: var(--pico-card-background-color);
}

/* Mobile first */
.footer-nav {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  align-items: center;
}

.footer-links {
  display: flex;
  gap: 1rem;
  padding: 0;
}

.footer-links li {
  list-style: none;
}

.footer-right {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  color: var(--pico-primary);
}

.app-version {
  font-size: 0.75rem;
  color: var(--pico-muted-color);
}

.icon-button {
  width: 2rem;
  height: 2rem;

  display: inline-flex;
  align-items: center;
  justify-content: center;

  padding: 0;
  border: 0;
  background: transparent;
  cursor: pointer;

  color: inherit;
  line-height: 1;
}

.icon-button > i {
  display: block;
  color: currentColor;
}

.icon-button:hover {
  color: var(--pico-muted-color);
}

/* Desktop */
@media (min-width: 640px) {
  .footer-nav {
    flex-direction: row;
    justify-content: space-between;
  }

  .footer-links {
    position: absolute;
    left: 50%;
    transform: translateX(-50%);
  }
}
</style>
