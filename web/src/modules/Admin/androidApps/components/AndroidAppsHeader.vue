<script setup lang="ts">
import { ANDROID_APPS_GRID } from '../types/adminAndroidApps.types'
</script>

<template>
  <div class="android-apps-header" :style="{ gridTemplateColumns: ANDROID_APPS_GRID }">
    <span />
    <span>Nom</span>
    <span>Paquet</span>
    <span title="Version proposée aux appareils">Sur le store</span>
    <span>Android min.</span>
    <span class="right">Taille</span>
    <span class="right" title="Versions importées">Versions</span>
    <span>Dernier import</span>
    <span class="center" title="Une app désactivée est retirée du store sans rien supprimer">Active</span>
    <span />
  </div>
</template>

<style scoped lang="scss">
.android-apps-header {
  display: grid;
  align-items: center;
  column-gap: 0.6rem;
  padding: 0 0.6rem;
  font-size: 0.75rem;
  color: var(--pico-muted-color);

  span { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
}

.right { text-align: right; }
.center { text-align: center; }
</style>
