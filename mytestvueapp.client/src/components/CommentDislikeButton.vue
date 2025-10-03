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
import CommentAccessService from "@/services/CommentAccessService";
import CommentDislikeService from "@/services/CommentDislikeService";
import { useToast } from "primevue/usetoast";

import { ref, onMounted, watch } from "vue";

const props = defineProps<{
  commentId: number;
  commentdislikes: number;
  isLiked: boolean;
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
  if (!loggedIn.value) return;
  disliked.value = await CommentDislikeService.isCommentDisliked(props.commentId);
}

watch(
  () => props.commentdislikes,
  (newVal) => {
    localCommentDislike.value = newVal;
  }
);

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
  const success = await CommentDislikeService.removeCommentDislike(props.commentId);
  if (success) {
    disliked.value = false;
    localCommentDislike.value--;
  }
} else {
  const success = await CommentDislikeService.insertCommentDislike(props.commentId);
  if (success) {
    disliked.value = true;
    localCommentDislike.value++;
  }
}
}
</script>