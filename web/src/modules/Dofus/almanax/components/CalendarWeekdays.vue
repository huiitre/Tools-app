<script setup lang="ts">

</script>

<template>
  <div class="calendar-weekdays">
    <div>L</div>
    <div>Ma</div>
    <div>Me</div>
    <div>J</div>
    <div>V</div>
    <div>S</div>
    <div>D</div>
  </div>
</template>

<style lang="scss" scoped>
.calendar-weekdays {
  display: grid;
  grid-template-columns: repeat(7, 1fr);
  margin-bottom: 0.5rem;
  font-size: 0.75rem;
  text-transform: uppercase;
  letter-spacing: 0.08em;
  color: var(--pico-muted-color);

  > div {
    text-align: center;
    padding: 0.25rem 0;
  }
}
</style>
