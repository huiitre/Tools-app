<template>
  <main class="not-found">
    <h1>404 — Rien. Absolument rien.</h1>

    <p>
      Cette page a disparu.<br />
      Soit elle n’a jamais existé, soit elle a fait de très mauvais choix.
    </p>

    <img
      class="animation"
      src="https://media.giphy.com/media/3o7abldj0b3rxrZUxW/giphy.gif"
      alt="Lost Astronaut"
    />

    <router-link
      to="/"
      role="button"
      class="contrast"
    >
      🏠 Retour à l'accueil
    </router-link>
  </main>
</template>

<script lang="ts">
export default {
  name: 'NotFound',
}
</script>

<style scoped>
.not-found {
  min-height: 100%;
  padding: 2rem 1rem;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 1.25rem;
  text-align: center;
}

.animation {
  width: 280px;
  max-width: 100%;
}
</style>
