<template>
  <div class="flex align-items-center">
    <div class="flex-grow-1">
      <div class="flex gap-3">
        <span v-if="comment.currentUserIsOwner">
          <i class="pi pi-star-fill" style="color: yellow"></i>
        </span>

        <span
          style="font-weight: bold"
          :style="{
            textDecoration: hover ? 'underline' : 'none',
            cursor: hover ? 'pointer' : 'none'
          }"
          class="py-1 font-semibold"
          @click="router.push(`/accountpage/${comment.commenterName}`)"
          v-on:mouseover="hover = true"
          v-on:mouseleave="hover = false"
          >{{ comment.commenterName }}</span
        >
        <span style="font-style: italic; color: gray">{{ dateFormatted }}</span>
      </div>
      <div class="ml-2">
        <span v-if="!editing" style="word-break: break-word">
          {{ comment.message }}
        </span>
        <div v-else>
          <InputText
            v-model:="newMessage"
            placeholder="Add a comment..."
            class="w-full mt-2"
          />
          <div class="flex flex-row-reverse mt-2 gap-2">
            <Button
              label="Submit"
              @click="submitEdit"
              :disabled="newMessage == ''"
            />
            <Button
              label="Cancel"
              severity="secondary"
              @click="editing = false"
            />
          </div>
        </div>
      </div>
      <div class="comment-actions">
        <!-- Reply to comments -->
        <Button
          @click="showReply = true"
          icon="pi pi-comment"
          rounded
          text
          label="Reply"
          severity="secondary"
        />
        <CommentLikeButton
          :comment-id="props.comment.id!"
          :comment-likes="props.comment.numLikes"
          :is-disliked="isCommentDisliked"
          @liked="isCommentLiked = true"
          @unliked="isCommentLiked = false"
        ></CommentLikeButton>
        <CommentDislikeButton
          :comment-id="props.comment.id!"
          :comment-dislikes="props.comment.numDislikes"
          :is-liked="isCommentLiked"
          @disliked="isCommentDisliked = true"
          @undisliked="isCommentDisliked = false"
        ></CommentDislikeButton>
        <Button
          v-if="comment.currentUserIsOwner || user"
          icon="pi pi-ellipsis-h"
          rounded
          text
          severity="secondary"
          @click="openMenu()"
        />
        <NewComment
          v-if="showReply == true"
          class="ml-4 mb-2"
          :parent-comment="comment"
          @new-comment="emit('deleteComment'), (showReply = false)"
          @close-reply="showReply = false"
        />

        <!-- Show replies to comments -->
        <!-- <Button class="ml-3 mb-2" @click="">Show Replies</Button> -->
      </div>
    </div>
    <div style="width: 5rem">
      <Menu ref="menu" :model="items" :popup="true" />
    </div>
  </div>
  <div class="ml-4 pl-3 mb-2" style="border-left: solid 1px gray">
    <CommentOnArt
      v-for="Comment in comment.replies"
      :key="Comment.id"
      :comment="Comment"
      @delete-comment="emit('deleteComment')"
    />
  </div>
</template>

<script setup lang="ts">
import CommentAccessService from "../../services/CommentAccessService";

import Comment from "@/entities/Comment";
import { ref, watch, onMounted } from "vue";
import Button from "primevue/button";
import InputText from "primevue/inputtext";
import Menu from "primevue/menu";
import { useToast } from "primevue/usetoast";
import NewComment from "./NewComment.vue";
import LoginService from "../../services/LoginService";
import router from "@/router";
import CommentLikeButton from "../CommentLikeButton.vue";
import CommentDislikeButton from "../CommentDislikeButton.vue";
import CommentLikeService from "@/services/CommentLikeService";
import CommentDislikeService from "@/services/CommentDislikeService";

const emit = defineEmits(["deleteComment"]);

const editing = ref<boolean>(false);
const newMessage = ref<string>("");
const toast = useToast();
const showReply = ref<boolean>(false);
const menu = ref<any>();
const user = ref<boolean>(false);
const dateFormatted = ref<string>("");
const hover = ref<boolean>(false);
const isCommentLiked = ref<boolean>(false);
const isCommentDisliked = ref<boolean>(false);

function openMenu(): void {
  menu.value.toggle(event);
}
interface icons {
  label: string;
  icon: string;
  command: () => void;
}

const items = ref<icons[]>([
  {
    label: "Delete",
    icon: "pi pi-trash",
    command: () => {
      deleteComment();
    }
  },
  {
    label: "Edit",
    icon: "pi pi-pencil",
    command: () => {
      editing.value = true;
    }
  }
]);

watch(editing, () => {
  newMessage.value = props.comment.message ?? "";
});

onMounted(async () => {
  getIsAdmin();

  const creationDate = adjustForTimezone(new Date(props.comment.creationDate));
  const today = new Date();

  const differenceMs = today.getTime() - creationDate.getTime();
  const differenceMinutes = Math.round(differenceMs / (1000 * 60));

  dateFormatted.value = getRelativeTime(differenceMinutes);

  if (await LoginService.isLoggedIn()) {
    isCommentLiked.value = await CommentLikeService.isCommentLiked(
      props.comment.numLikes
    );
    isCommentDisliked.value = await CommentDislikeService.isCommentDisliked(
      props.comment.numDislikes
    );
  }
});

const props = defineProps<{
  comment: Comment;
}>();

async function getIsAdmin(): Promise<void> {
  LoginService.getIsAdmin().then((promise: boolean) => {
    user.value = promise;
  });
}

async function submitEdit(): Promise<void> {
  if (props.comment.id != null) {
    CommentAccessService.editComment(props.comment, newMessage.value)
      .then(() => {
        emit("deleteComment");
        editing.value = false;
      })
      .catch(() => {
        toast.add({
          severity: "error",
          summary: "Error",
          detail: "Failed to edit comment",
          life: 3000
        });
      });
  }
}

async function deleteComment(): Promise<void> {
  if (props.comment.id != null) {
    CommentAccessService.deleteComment(props.comment.id).then(() => {
      emit("deleteComment");
    });
  }
}

function adjustForTimezone(date: Date): Date {
  let timeOffsetInMS: number = date.getTimezoneOffset() * 60000;
  date.setTime(date.getTime() - timeOffsetInMS);
  return date;
}

function getRelativeTime(minutes: number): string {
  if (minutes === 0) return `Just now`;
  if (minutes < 60) return `${minutes} minute${minutes > 1 ? "s" : ""} ago`;
  if (minutes < 1440)
    return `${Math.floor(minutes / 60)} hour${
      Math.floor(minutes / 60) > 1 ? "s" : ""
    } ago`;

  const days = Math.round(minutes / (60 * 24));

  if (days < 7) return `${days} day${days > 1 ? "s" : ""} ago`;
  if (days < 30)
    return `${Math.floor(days / 7)} week${
      Math.floor(days / 7) > 1 ? "s" : ""
    } ago`;
  if (days < 365)
    return `${Math.floor(days / 30.437)} month${
      Math.floor(days / 30.437) > 1 ? "s" : ""
    } ago`;

  const years = Math.floor(days / 365);
  return `${years} year${years > 1 ? "s" : ""} ago`;
}
</script>
<style scoped>
.comment-actions {
  display: flex;
  align-items: center;
  gap: .5rem; /* space between action buttons */
  margin-top: .5rem;
}

/* Ensure reply/new-comment spacing looks good when NewComment is shown inline */
.comment-actions :global(.ml-4) {
  margin-left: 1rem !important;
}

/* Slightly reduce spacing for very small screens */
@media (max-width: 420px) {
  .comment-actions { gap: .35rem; }
}
</style>
