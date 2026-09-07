<script setup lang="ts">
import { onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/modules/Auth/auth.store'
import { useFetchMe } from '@/modules/Auth/fetch/auth.fetch'
import toast from '@/services/toast'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()

// Codes renvoyés par /auth/callback/google quand la connexion échoue. L'API redirige ici plutôt
// que de répondre une erreur : le callback est atteint par le navigateur, une réponse JSON
// laisserait l'utilisateur sur une page vide.
const GOOGLE_ERRORS: Record<string, string> = {
  USER_DISABLED: 'Ce compte a été désactivé.',
  GOOGLE_EMAIL_ALREADY_REGISTERED: 'Un compte existe déjà avec cette adresse email.',
  // Le state ne vit que quelques minutes et ne sert qu'une fois : c'est un lien rejoué ou
  // laissé de côté trop longtemps, pas un refus.
  GOOGLE_STATE_INVALID: 'Lien de connexion expiré, merci de réessayer.',
}

onMounted(async () => {
  if (auth.user) {
    return router.replace('/')
  }

  const error = route.query.error as string | undefined

  if (error) {
    // Un code inconnu reste un échec de connexion : le message générique le couvre.
    toast.error(GOOGLE_ERRORS[error] ?? 'Erreur lors de la connexion Google')
    return router.replace('/login')
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