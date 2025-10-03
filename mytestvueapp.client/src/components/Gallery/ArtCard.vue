<template>
  <div class="mr-4 mb-4 border-round-md">
    <Card
      class="art-card flex-shrink-0 overflow-hidden border-round-md cursor-pointer p-0 gallery-card"
      @click="router.push(`/art/${art.id}`)">
      <template #header>
        <MyCanvas
          :art="art"
          :pixelSize="size"
          :canvasNumber="position"
          :model-value="'temp'" />
      </template>

      <template #title>
        <div class="text-base font-bold m-0 px-2 pt-1">
          {{ art.title }}
        </div>
      </template>

      <template #subtitle>
        <div class="text-sm m-0 px-2 max-w-11rem text-overflow-ellipsis">
          <div
            v-for="(artist, index) in art.artistName"
            :key="index"
            class="py-1 font-semibold">
            {{ artist }}
          </div>
        </div>
      </template>

      <template #content>
        <!-- Tag pills -->
        <ul
          v-if="art.tags && art.tags.length"
          class="tag-list px-2 pt-1 pb-0">
          <li
            v-for="t in limitedTags"
            :key="t.id || t.name"
            class="tag-pill"
            :title="t.name">
            {{ t.name }}
          </li>
          <li
            v-if="overflowCount > 0"
            class="tag-pill more"
            :title="overflowTitle">
            +{{ overflowCount }}
          </li>
        </ul>

        <div class="flex gap-2 m-2 mt-2">
          <LikeButton :artId="props.art.id" :likes="props.art.numLikes" />
          <Button
            rounded
            severity="secondary"
            icon="pi pi-comment"
            :label="art.numComments?.toString() || ''"
          />
          <Button
            v-if="art.isGif"
            rounded
            severity="secondary"
            disabled>
            Gif
          </Button>
        </div>
      </template>
    </Card>
  </div>
</template>

<script setup lang="ts">
import { defineProps, computed } from "vue";
import Card from "primevue/card";
import Button from "primevue/button";
import LikeButton from "../LikeButton.vue";
import MyCanvas from "../MyCanvas/MyCanvas.vue";
import Art from "@/entities/Art";
import router from "@/router";

const props = defineProps<{
  art: Art;
  size: number;
  position: number;
}>();

const limitedTags = computed(() => (props.art.tags || []).slice(0, 6));
const overflowCount = computed(() =>
  (props.art.tags?.length || 0) > 6 ? (props.art.tags.length - 6) : 0
);
const overflowTitle = computed(() =>
  (props.art.tags || []).slice(6).map(t => t.name).join(", ")
);
</script>

<style scoped>
.art-card {
  border: 1px solid transparent;
}
.art-card:hover {
  border: 1px solid var(--p-primary-color);
}

.tag-list {
  list-style: none;
  margin: 0;
  padding: 0 0 .25rem 0;
  display: flex;
  flex-wrap: wrap;
  gap: .35rem;
}

.tag-pill {
  background: #2a313a;
  color: #fff;
  font-size: .55rem;
  font-weight: 600;
  padding: .30rem .55rem;
  line-height: 1;
  border-radius: 9999px;
  border: 1px solid #434b55;
  max-width: 110px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  user-select: none;
  transition: .15s;
}
.tag-pill.more {
  background: #444155;
}
.tag-pill:hover {
  background: #4b5563;
  border-color: #6b7280;
}
</style>

<style>
.gallery-card .p-card-body {
  padding: 0 !important;
  gap: 0 !important;
}
.gallery-card .p-card-caption {
  gap: 0 !important;
}
</style>

