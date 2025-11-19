<template>
  <Button
    class="comment-vote-like"
    rounded
    :severity="liked ? 'primary' : 'secondary'"
    :icon="liked ? 'pi pi-thumbs-up-fill' : 'pi pi-thumbs-up'"
    :label="(commentLikes + localCommentLike).toString()"
    @click.stop="likedClicked()"
  />
</template>
<script setup lang="ts">
import Button from "primevue/button";
//import CommentAccessService from "@/services/CommentAccessService";
import LoginService from "@/services/LoginService";
import CommentLikeService from "@/services/CommentLikeService";
import { useToast } from "primevue/usetoast";

import { ref, onMounted, watch } from "vue";

const props = defineProps<{
  commentId: number;
  commentLikes: number;
  isDisliked: boolean;
}>();
const loggedIn = ref<boolean>(false);
const localCommentLike = ref<number>(0);
const liked = ref<boolean>(false);

const emit = defineEmits(["liked", "unliked"]);

const toast = useToast();

onMounted(async () => {
  await LoginService.isLoggedIn().then((value) => (loggedIn.value = value));
  isLiked();
  localCommentLike.value = 0;
});

async function isLiked(): Promise<void> {
  if (loggedIn.value)
    CommentLikeService.isCommentLiked(props.commentId).then(
      (value) => (liked.value = value)
    );
}

watch(
  () => props.commentId,
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
      detail: "User must be logged in to like a comment!",
      life: 3000
    });
    return;
  }
  if (liked.value) {
    const success = await CommentLikeService.removeCommentLike(props.commentId);
    if (success) {
      liked.value = false;
      emit("unliked");
      if (localCommentLike.value >= 0) {
        localCommentLike.value--;
      }
    }
  } else {
    const success = await CommentLikeService.insertCommentLike(props.commentId);
    if (success) {
      liked.value = true;
      emit("liked");
      if (localCommentLike.value <= 0) {
        localCommentLike.value++;
      }
    }
  }
}
</script>
