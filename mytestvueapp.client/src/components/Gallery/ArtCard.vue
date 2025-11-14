<template>
  <div class="mr-4 mb-4 border-round-md feed-card-wrap">
    <Card
      class="art-card flex-shrink-0 overflow-hidden border-round-md cursor-pointer p-0 gallery-card"
      @click="router.push(`/art/${art.id}`)"
    >
      <template #header>
        <div class="media-wrap">
          <MyCanvas
            :art="art"
            :pixelSize="size"
            :canvasNumber="position"
            :model-value="'temp'"
          />
        </div>
      </template>

      <template #title>
        <div class="title-row text-base font-bold m-0 px-2 pt-1">
          {{ title }}
        </div>
      </template>

      <template #subtitle>
        <div class="subtitle-row text-sm m-0 px-2 max-w-11rem text-overflow-ellipsis">
          <div v-if="artistNames.length > 1">
            <button class="artist-link" @click.stop="goToArtist(artistNames[0])">
              {{ artistNames[0] }}
            </button>, etc.
          </div>
          <div v-else-if="artistNames.length === 1">
            <button class="artist-link" @click.stop="goToArtist(artistNames[0])">
              {{ artistNames[0] }}
            </button>
          </div>
          <div v-else></div>
        </div>
      </template>

      <template #content>
        <!-- Tag pills -->
        <ul
          v-if="art.tags && art.tags.length"
          class="tag-list px-2 pt-1 pb-0 tag-height"
        >
          <li
            v-for="t in limitedTags"
            :key="t.id || t.name"
            class="tag-pill"
            :title="t.name"
            @click.stop="router.push({ name: 'TagGallery', params: { tag: t.name } })"
          >
            {{ t.name }}
          </li>
          <li
            v-if="overflowCount > 0"
            class="tag-pill more"
            :title="overflowTitle"
          >
            +{{ overflowCount }}
          </li>
        </ul>
        <ul v-else class="tag-height"></ul>

        <div class="flex gap-2 m-2 mt-2 gap-x-2 justify-content-center align-items-center">
          <LikeButton
            :artId="props.art.id"
            :likes="props.art.numLikes ?? 0"
            :is-disliked="isDisliked"
            @liked="isLiked = true"
            @unliked="isLiked = false"
          />
          <DislikeButton
            :artId="props.art.id"
            :dislikes="props.art.numDislikes ?? 0"
            :is-liked="isLiked"
            @disliked="isDisliked = true"
            @undisliked="isDisliked = false"
          />
          <Button
            rounded
            severity="secondary"
            icon="pi pi-comment"
            :label="numComments.toString() || ''"
          />
        </div>
      </template>
    </Card>
  </div>
</template>

<script setup lang="ts">
import { defineProps, ref, computed } from "vue";
import Card from "primevue/card";
import Button from "primevue/button";
import LikeButton from "../LikeButton.vue";
import DislikeButton from "../DislikeButton.vue";
import MyCanvas from "../MyCanvas/MyCanvas.vue";
import Art from "@/entities/Art";
import router from "@/router";

//const title = ref<string>("");
const isLiked = ref<boolean>(false);
const isDisliked = ref<boolean>(false);

const props = defineProps<{
  art: Art;
  size: number;
  position: number;
}>();

const limitedTags = computed(() => (props.art.tags || []).slice(0, 6));
const overflowCount = computed(() =>
  (props.art.tags?.length || 0) > 6 ? props.art.tags.length - 6 : 0
);
const overflowTitle = computed(() =>
  (props.art.tags || [])
    .slice(6)
    .map((t) => t.name)
    .join(", ")
);
const title = computed(() => {
  const t = props.art.title ?? "";
  if (t.length > 32) {
    const tempTitle = t.substring(0, 32);
    const ellipsis = "...";
    return tempTitle + ellipsis;
  } else {
    return t;
  }
});
const numComments = computed(() => {
  let commentLabel = "";
  const tempComments = props.art.numComments ?? 0;
  if (tempComments > 999999) {
    commentLabel = Math.floor(tempComments / 1000000) + "M";
  } else if (tempComments > 999) {
    commentLabel = Math.floor(tempComments / 1000) + "K";
  } else {
    commentLabel = tempComments + "";
  }
  return commentLabel;
});
const artistNames = computed<string[]>(() => props.art.artistName ?? []);

function goToArtist(name: string) {
  router.push(`/accountpage/${encodeURIComponent(name)}`);
}
</script>

<style scoped>
.art-card { border: 1px solid transparent; }
.art-card:hover { border: 1px solid var(--p-primary-color); }

/* Ensure parent spacing is controlled by the feed gap, not margins on the card wrapper */
.feed-card-wrap {
  margin-right: 0 !important;
  margin-bottom: 0 !important;
  width: 100%;
}

/* Media area: responsive square (keeps rows aligned) */
.media-wrap {
  position: relative;
  width: 100%;
  aspect-ratio: 1 / 1;
  overflow: hidden;
}

/* Fallback if aspect-ratio isn’t supported */
@supports not (aspect-ratio: 1 / 1) {
  .media-wrap::before { content: ""; display: block; padding-top: 100%; }
  .media-wrap > * { position: absolute; inset: 0; }
}

/* Remove Card paddings that introduce gutters */
:deep(.gallery-card .p-card-header),
:deep(.gallery-card .p-card-body),
:deep(.gallery-card .p-card-content),
:deep(.gallery-card .p-card-footer) { padding: 0 !important; }

/* Fit the canvas into the media area */
:deep(.gallery-card canvas) {
  position: absolute;
  inset: 0;
  width: 100% !important;
  height: 100% !important;
  display: block;
  image-rendering: pixelated;
  image-rendering: crisp-edges;
}

/* Fix title/subtitle heights so rows align across cards */
.title-row {
  min-height: 28px;           /* ~1 line */
  display: flex;
  align-items: center;
}
.subtitle-row {
  min-height: 22px;           /* ~1 line */
  display: flex;
  align-items: center;
}

/* Tag list already fixed with .tag-height */
.tag-list {
  list-style: none;
  margin: 0;
  padding: 0 0 0.25rem 0;
  display: flex;
  flex-wrap: wrap;
  gap: 0.35rem;
}
.tag-pill {
  background: var(--tag-bg);
  color: var(--tag-text);
  font-size: 0.55rem;
  font-weight: 600;
  padding: 0.3rem 0.55rem;
  line-height: 1;
  border-radius: 9999px;
  border: 1px solid var(--tag-border);
  max-width: 110px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  user-select: none;
  transition: 0.15s;
  cursor: pointer;
}
.tag-pill.more { background: var(--tag-more-bg); }
.tag-pill:hover { background: var(--tag-hover-bg); border-color: var(--tag-hover-border); }

/* Username link styles */
.artist-link {
  background: transparent; border: none; padding: 0;
  color: #9ca3af; font-weight: 700; text-decoration: none; cursor: pointer;
  transition: color 0.15s ease, text-decoration-color 0.15s ease;
}
.artist-link:hover, .artist-link:focus-visible {
  text-decoration: underline; text-underline-offset: 2px;
}

/* Mobile keeps full-width; desktop width is controlled by the feed container */
@media (max-width: 767px) {
  .gallery-card { width: 100%; }
}
</style>

<style>
/* Keep PrimeVue internals tight */
.gallery-card .p-card-body { padding: 0 !important; gap: 0 !important; }
.gallery-card .p-card-caption { gap: 0 !important; }
.tag-height { height: 24px !important; margin: 0 !important; }
</style>

<!-- 4 -->