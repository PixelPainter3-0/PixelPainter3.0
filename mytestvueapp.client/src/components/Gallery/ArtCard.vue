<template>
  <div class="mr-4 mb-4 border-round-md">
    <!-- Container -->
    <Card
      class="art-card flex-shrink-0 overflow-hidden border-round-md cursor-pointer p-0 gallery-card"
      @click="router.push(`/art/${art.id}`)"
    >
      <template #header>
        <div
          class="justify-content-center flex w-full h-full align-items-center"
        >
          <MyCanvas
            :art="art"
            :pixelSize="size"
            :canvasNumber="position"
            :model-value="'temp'"
          />
        </div>
      </template>
      <template #title>
        <div class="text-base font-bold m-0 px-2 pt-1">
          {{ title }}
        </div>
      </template>
      <template #subtitle>
        <div class="text-sm m-0 px-2 max-w-11rem text-overflow-ellipsis">
          <div
            v-for="(artist, index) in art.artistName"
            :key="index"
            class="py-1 font-semibold"
            onclick="//thing to route"
          >
            {{ artist }}
          </div>
        </div>
      </template>
      <template #content>
        <div class="flex gap-2 m-2">
          <LikeButton
            :artId="props.art.id"
            :likes="props.art.numLikes"
            :is-disliked="isDisliked"
            @liked="isLiked = true"
            @unliked="isLiked = false"
          />
          <DislikeButton
            :artId="props.art.id"
            :dislikes="props.art.numDislikes"
            :is-liked="isLiked"
            @disliked="isDisliked = true"
            @undisliked="isDisliked = false"
          />
          <Button
            rounded
            severity="secondary"
            icon="pi pi-comment"
            :label="art.numComments?.toString() || ''"
          />
          <Button v-if="art.isGif" rounded severity="secondary" disabled
            >Gif</Button
          >
        </div>
      </template>
    </Card>
  </div>
</template>

<script setup lang="ts">
import { defineProps, ref, onMounted } from "vue";
import Card from "primevue/card";
import Button from "primevue/button";
import LikeButton from "../LikeButton.vue";
import DislikeButton from "../DislikeButton.vue";
import MyCanvas from "../MyCanvas/MyCanvas.vue";
import Art from "@/entities/Art";
import router from "@/router";

const title = ref<string>("");
const isLiked = ref<boolean>(false);
const isDisliked = ref<boolean>(false);

onMounted(() => {
  if (props.art.title.length > 20) {
    const tempTitle = props.art.title.substring(0, 20);
    const elipsis = "...";
    title.value = tempTitle + elipsis;
  } else {
    title.value = props.art.title;
  }
});

const props = defineProps<{
  art: Art;
  size: number;
  position: number;
}>();
</script>

<style scoped>
.art-card {
  border: 1px solid transparent;
}

.art-card:hover {
  border: 1px solid var(--p-primary-color);
}
</style>

<style>
.gallery-card .p-card-body {
  padding: 0px !important;
  gap: 0px !important;
}

.gallery-card .p-card-caption {
  gap: 0px !important;
}
</style>
