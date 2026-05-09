<script setup lang="ts">
import WorkshopCreateBar from '@/modules/Dofus/workshop/components/WorkshopCreateBar.vue'
import WorkshopList from '@/modules/Dofus/workshop/components/WorkshopList.vue'

const props = defineProps<{
  tagId?: string
}>()

</script>

<template>
  <WorkshopCreateBar />
  <WorkshopList />
</template>
