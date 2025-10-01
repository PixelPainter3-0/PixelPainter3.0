<template>
  <Button
    rounded
    :severity="liked ? 'primary' : 'secondary'"
    :icon="liked ? 'pi pi-thumbs-up-fill' : 'pi pi-thumbs-up'"
    :label="(likes + localLike).toString()"
    @click.stop="likedClicked()"
  />
</template>
<script setup lang="ts">
import Button from "primevue/button";
import LoginService from "@/services/LoginService";
import LikeService from "@/services/LikeService";
import { useToast } from "primevue/usetoast";

import { ref, onMounted, watch, defineEmits } from "vue";

const props = defineProps<{
  likes: number;
  artId: number;
  isDisliked: boolean;
}>();

const localLike = ref<number>(0);
const liked = ref<boolean>(false);
const loggedIn = ref<boolean>(false);

const emit = defineEmits(["liked", "unliked"]);

const toast = useToast();

onMounted(async () => {
  await LoginService.isLoggedIn().then((value) => (loggedIn.value = value));
  isLiked();
  localLike.value = 0;
});

async function isLiked(): Promise<void> {
  if (loggedIn.value)
    LikeService.isLiked(props.artId).then((value) => (liked.value = value));
}

watch(
  () => props.artId,
  async () => {
    await isLiked();
  }
);
watch(
  () => props.isDisliked,
  async () => {
    if (liked.value && props.isDisliked) {
      likedClicked();
    }
  }
);

async function likedClicked(): Promise<void> {
  if (!loggedIn.value) {
    // Route to login page
    toast.add({
      severity: "error",
      summary: "Warning",
      detail: "User must be logged in to like art!",
      life: 3000
    });
    return;
  }
  if (liked.value) {
    // Try to unlike
    LikeService.removeLike(props.artId).then((value) => {
      if (value) {
        liked.value = false;
        emit("unliked");
      }
      if (localLike.value >= 0) {
        localLike.value--;
      }
    });
  } else {
    // Try to Like
    LikeService.insertLike(props.artId).then((value) => {
      if (value) {
        liked.value = true;
        emit("liked");
      }
      if (localLike.value <= 0) {
        localLike.value++;
      }
    });
  }
}
</script>
