<template>
  <Button
    rounded
    :severity="disliked ? 'primary' : 'secondary'"
    :icon="disliked ? 'pi pi-thumbs-down-fill' : 'pi pi-thumbs-down'"
    :label="(displayDislikes).toString()"
    @click.stop="dislikedClicked()"
  />
</template>
<script setup lang="ts">
import Button from "primevue/button";
import LoginService from "@/services/LoginService";
import DislikeService from "@/services/DislikeService";
import { useToast } from "primevue/usetoast";

import { ref, onMounted, watch, defineEmits, computed } from "vue";

const props = defineProps<{
  artId: number;
  dislikes: number;
  isLiked: boolean;
}>();
const loggedIn = ref<boolean>(false);
const localDislike = ref<number>(0);
const disliked = ref<boolean>(false);

const emit = defineEmits(["disliked", "undisliked"]);

const toast = useToast();

onMounted(async () => {
  await LoginService.isLoggedIn().then((value) => (loggedIn.value = value));
  isDisliked();
  localDislike.value = 0;
});

async function isDisliked(): Promise<void> {
  if (loggedIn.value)
    DislikeService.isDisliked(props.artId).then(
      (value) => (disliked.value = value)
    );
}

const displayDislikes = computed(() => {
  let dislikesLabel = "";
  const tempDislikes = props.dislikes + localDislike.value;
  if(tempDislikes > 999999)
  {
    dislikesLabel = Math.floor(tempDislikes / 1000000) + "M";
    return dislikesLabel;
  }
  else if (tempDislikes > 999)
  {
      dislikesLabel = Math.floor(tempDislikes / 1000) + "K";
      return dislikesLabel;
  }
  else
  {
    dislikesLabel = (tempDislikes) + "";
    return dislikesLabel;
  }
});

watch(
  () => props.artId,
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
        emit("undisliked");
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
        emit("disliked");
      }
      if (localDislike.value <= 0) {
        localDislike.value++;
      }
    });
  }
}
</script>
