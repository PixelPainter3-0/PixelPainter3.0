<template>
  <Button
    rounded
    :severity="disliked ? 'primary' : 'secondary'"
    :icon="disliked ? 'pi pi-thumbs-down-fill' : 'pi pi-thumbs-down'"
    :label="(commentdislikes + localCommentDislike).toString()"
    @click.stop="dislikedClicked()"
  />
</template>
<script setup lang="ts">
import Button from "primevue/button";
import LoginService from "@/services/LoginService";
import CommentDislikeService from "@/services/CommentDislikeService";
import { useToast } from "primevue/usetoast";

import { ref, onMounted, watch } from "vue";

const props = defineProps<{
  commentId: number;
  commentdislikes: number;
}>();
const loggedIn = ref<boolean>(false);
const localCommentDislike = ref<number>(0);
const disliked = ref<boolean>(false);

const toast = useToast();

onMounted(async () => {
  await LoginService.isLoggedIn().then((value) => (loggedIn.value = value));
  isDisliked();
  localCommentDislike.value = 0;
});

async function isDisliked(): Promise<void> {
  if (loggedIn.value)
    CommentDislikeService.isCommentDisliked(props.commentId).then((value) => (disliked.value = value));
}

watch(
  () => props.commentId,
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
    CommentDislikeService.removeCommentDislike(props.commentId).then((value) => {
      if (value) {
        disliked.value = false;
      }
      if (localCommentDislike.value >= 0) {
        localCommentDislike.value--;
      }
    });
  } else {
    // Try to Dislike
    CommentDislikeService.insertCommentDislike(props.commentId).then((value) => {
      if (value) {
        disliked.value = true;
      }
      if (localCommentDislike.value <= 0) {
        localCommentDislike.value++;
      }
    });
  }
}
</script>