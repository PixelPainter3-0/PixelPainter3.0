<template>
  <Button
    rounded
    :severity="liked ? 'primary' : 'secondary'"
    :icon="liked ? 'pi pi-thumbs-up-fill' : 'pi pi-thumbs-up'"
    :label="(commentlikes + localCommentLike).toString()"
    @click.stop="likedClicked()"
  />
</template>
<script setup lang="ts">
import Button from "primevue/button";
import LoginService from "@/services/LoginService";
import CommentLikeService from "@/services/CommentLikeService";
import { useToast } from "primevue/usetoast";

import { ref, onMounted, watch } from "vue";

const props = defineProps<{
  commentId: number;
  commentlikes: number;
  isLiked: boolean;
}>();
const loggedIn = ref<boolean>(false);
const localCommentLike = ref<number>(0);
const liked = ref<boolean>(false);

const toast = useToast();

onMounted(async () => {
  await LoginService.isLoggedIn().then((value) => (loggedIn.value = value));
  isLiked();
  localCommentLike.value = 0;
});

async function isLiked(): Promise<void> {
  if (loggedIn.value)
    CommentLikeService.isCommentLiked(props.commentId).then((value) => (liked.value = value));
}

watch(
  () => props.commentId,
  async () => {
    await isLiked();
  }
);

async function likedClicked(): Promise<void> {
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
  if (liked.value) {
    // Try to unlike
    CommentLikeService.removeCommentLike(props.commentId).then((value) => {
      if (value) {
        liked.value = false;
      }
      if (localCommentLike.value >= 0) {
        localCommentLike.value--;
      }
    });
  } else {
    // Try to like
    CommentLikeService.insertCommentLike(props.commentId).then((value) => {
      if (value) {
        liked.value = true;
      }
      if (localCommentLike.value <= 0) {
        localCommentLike.value++;
      }
    });
  }
}
</script>