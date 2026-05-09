<script setup lang="ts">
import { onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import toast from '@/services/toast'
import { useFetchVerifyEmail } from '@/modules/Auth/fetch/auth.fetch'

const route = useRoute()
const router = useRouter()

onMounted(async () => {
  const token = route.query.token as string | undefined

  if (!token) {
    router.back()
    return
  }

  try {
    const { data } = await useFetchVerifyEmail(token)
    toast.success(data.message)
    router.push('/login')
  } catch (error: any) {
    toast.error(error?.message || 'Lien de validation invalide ou expiré.')
    router.push('/login')
  }
})
</script>

<template>
  <main class="verify-email">
    <p>Validation de votre adresse email en cours…</p>
  </main>
</template>

<style scoped>
.verify-email {
  width: 100%;
  padding: 2rem 1rem;
  display: flex;
  justify-content: center;
  font-size: 1rem;
  opacity: 0.7;
}
</style>
