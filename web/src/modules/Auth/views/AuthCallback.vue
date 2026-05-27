<script setup lang="ts">
import { onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/modules/Auth/auth.store'
import { useFetchMe } from '@/modules/Auth/fetch/auth.fetch'
import toast from '@/services/toast'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()

onMounted(async () => {
  if (auth.user) {
    return router.replace('/')
  }

  const token = route.query.token as string | undefined

  if (token) {
    try {
      auth.setToken(token)
      const me = await useFetchMe()
      auth.setUser(me.data)
      router.replace('/')
    } catch {
      toast.error('Erreur lors de la connexion Google')
      router.replace('/login')
    }
  } else {
    router.replace('/login')
  }
})
</script>

<template>
  <div></div>
</template>