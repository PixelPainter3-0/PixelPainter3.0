<template>
  <Button
    class="comment-vote-dislike"
    rounded
    :severity="disliked ? 'primary' : 'secondary'"
    :icon="disliked ? 'pi pi-thumbs-down-fill' : 'pi pi-thumbs-down'"
    :label="(commentDislikes + localCommentDislike).toString()"
    @click.stop="dislikedClicked()"
  />
</template>
<script setup lang="ts">
import Button from "primevue/button";
import LoginService from "@/services/LoginService";
import CommentDislikeService from "@/services/CommentDislikeService";
import { useToast } from "primevue/usetoast";

import { ref, onMounted, watch, defineEmits } from "vue";

const props = defineProps<{
  commentId: number;
  commentDislikes: number;
  isLiked: boolean;
}>();
const loggedIn = ref<boolean>(false);
const localCommentDislike = ref<number>(0);
const disliked = ref<boolean>(false);

const emit = defineEmits(["disliked", "undisliked"]);

const toast = useToast();

onMounted(async () => {
  await LoginService.isLoggedIn().then((value) => (loggedIn.value = value));
  isDisliked();
  localCommentDislike.value = 0;
});

async function isDisliked(): Promise<void> {
  if (loggedIn.value)
    CommentDislikeService.isCommentDisliked(props.commentId).then(
      (value) => (disliked.value = value)
    );
}

watch(
  () => props.commentId,
  async () => {
    await isDisliked();
  }
);
watch(
  () => props.isLiked,
  async () => {
    if (disliked.value && props.isLiked) {
      dislikedClicked();
    }
  }
);

async function dislikedClicked(): Promise<void> {
  if (!loggedIn.value) {
    // Route to login page
    toast.add({
      severity: "error",
      summary: "Warning",
      detail: "User must be logged in to dislike a comment!",
      life: 3000
    });
    return;
  }
  if (disliked.value) {
    const success = await CommentDislikeService.removeCommentDislike(
      props.commentId
    );
    if (success) {
      disliked.value = false;
      localCommentDislike.value--;
      emit("undisliked");
    }
  } else {
    const success = await CommentDislikeService.insertCommentDislike(
      props.commentId
    );
    if (success) {
      disliked.value = true;
      localCommentDislike.value++;
      emit("disliked");
    }
  }
}
</script>

<style scoped>
/* Add external space between the like and dislike buttons (no inner padding) */
.comment-vote-dislike {
  margin: 0.5rem 1rem 1rem 1rem;
}
</style>
