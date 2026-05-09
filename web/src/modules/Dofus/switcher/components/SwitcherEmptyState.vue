<template>
  <div class="empty">
    <span class="mdi mdi-ghost-outline" />
    <span>Aucune fenêtre Dofus détectée</span>
  </div>
</template>

<style lang="scss" scoped>
.empty {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 10px;
  height: 200px;
  color: rgba(255, 255, 255, 0.25);
  font-size: 13px;

  .mdi {
    font-size: 36px;
  }
}
</style>