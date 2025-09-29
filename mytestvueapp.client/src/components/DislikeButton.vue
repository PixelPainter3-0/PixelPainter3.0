<template>
  <Button
    rounded
    :severity="disliked ? 'primary' : 'secondary'"
    :icon="disliked ? 'pi pi-thumbs-down-fill' : 'pi pi-thumbs-down'"
    :label="(dislikes + localDislike).toString()"
    @click.stop="dislikedClicked()"
  />
</template>
<script setup lang="ts">
import Button from "primevue/button";
import LoginService from "@/services/LoginService";
import DislikeService from "@/services/DislikeService";
import { useToast } from "primevue/usetoast";

import { ref, onMounted, watch } from "vue";

const props = defineProps<{
  artId: number;
  dislikes: number;
}>();
const loggedIn = ref<boolean>(false);
const localDislike = ref<number>(0);
const disliked = ref<boolean>(false);

const toast = useToast();

onMounted(async () => {
  await LoginService.isLoggedIn().then((value) => (loggedIn.value = value));
  isDisliked();
  localDislike.value = 0;
});

async function isDisliked(): Promise<void> {
  if (loggedIn.value)
    DislikeService.isDisliked(props.artId).then((value) => (disliked.value = value));
}

watch(
  () => props.artId,
  async () => {
    await isDisliked();
  }
);

async function dislikedClicked(): Promise<void> {
  if (!loggedIn.value) {
    // Route to login page
    toast.add({
      severity: "error",
      summary: "Warning",
      detail: "User must be logged in to dislike art!",
      life: 3000
    });
    return;
  }
  if (disliked.value) {
    // Try to undislike
    DislikeService.removeDislike(props.artId).then((value) => {
      if (value) {
        disliked.value = false;
      }
      if (localDislike.value >= 0) {
        localDislike.value--;
      }
    });
  } else {
    // Try to Dislike
    DislikeService.insertDislike(props.artId).then((value) => {
      if (value) {
        disliked.value = true;
      }
      if (localDislike.value <= 0) {
        localDislike.value++;
      }
    });
  }
}
</script>
