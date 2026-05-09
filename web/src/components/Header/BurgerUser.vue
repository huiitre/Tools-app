<script setup lang="ts">
const props = defineProps<{
  name: string
  email: string
  avatar?: string
}>()

</script>

<template>
  <div class="burger-user">
    <div class="avatar">
      <img
        v-if="avatar && (avatar.startsWith('http') || avatar.startsWith('/'))"
        :src="avatar"
        alt="Avatar utilisateur"
      />
      <span v-else>
        {{ name.charAt(0).toUpperCase() }}
      </span>
    </div>

    <div class="user-info">
      <strong>{{ name }}</strong>
      <small>{{ email }}</small>
    </div>
  </div>
</template>

<style scoped>
.burger-user {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  margin-top: 2rem;
}

.avatar {
  width: 40px;
  height: 40px;
  border-radius: 50%;

  overflow: hidden;

  background: var(--pico-muted-border-color);
  color: var(--pico-color);

  display: flex;
  align-items: center;
  justify-content: center;

  font-weight: 600;
}

.user-info {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;

  & small {
    color: var(--pico-muted-color);
    font-size: 0.8rem;
  }
}
</style>
