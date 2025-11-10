<template>
  <Button
    :title="isEditing ? 'Save Changes' : 'Upload'"
    :label="isHover ? (isEditing ? 'Save' : 'Upload') : ''"
    :icon="isEditing ? 'pi pi-save' : 'pi pi-upload'"
    @click="toggleModal()"
    @mouseover="isHover = true"
    @mouseleave="isHover = false"
    :disabled="tagCreationLoading || loading"
    :id="'uploadButton'"
  ></Button>

  <!---->
  <Dialog
    v-model:visible="visible"
    modal
    :style="{ width: '40rem', maxWidth: '95vw' }"
  >
    <template #header>
      <h1 class="mr-2">
        {{ isEditing ? "Save Your Changes?" : "Upload Your Art?" }}
      </h1>
    </template>
    <div class="flex flex-column gap-3 justify-content-center">
      <!-- title selection -->
      <div class="flex align-items-center gap-3">
        <span>Title: </span>
        <InputText
          v-model="newName"
          placeholder="Title"
          class="w-full"
        ></InputText>
      </div>

      <!-- tags (combined search + dropdown + checkboxes) -->
      <div class="flex align-items-center gap-3">
        <span>Tags:</span>
        <MultiSelect
          v-model="selectedTagIds"
          :options="displayedTags"
          optionLabel="name"
          optionValue="id"
          filter
          display="chip"
          placeholder="Search & select tags"
          :maxSelectedLabels="4"
          :selectionLimit="4"
          class="w-full min-w-15rem"
          @filter="onTagFilter"
          @change="onTagChange"
        />
      </div>

      <!-- tag creation -->
      <div class="flex align-items-center gap-3 mt-2">
        <span>New Tag:</span>
        <InputText
          v-model="newTagName"
          placeholder="New tag name.."
          class="w-10rem"
          :disabled="tagCreationLoading"
          :maxlength="10"
        />
        <Button
          label="Add Tag"
          size="small"
          :loading="tagCreationLoading"
          @click="createTag"
          :disabled="!newTagName || tagCreationLoading"
        />
      </div>
      <div
        class="text-xs text-color-secondary"
        v-if="selectedTagIds.length < 4"
      >
        {{ 4 - selectedTagIds.length }} tag(s) remaining*
      </div>
      <div class="text-xs text-color-secondary" v-else>
        Tag limit reached! (4)
      </div>
      <div v-if="tagCreationError" class="text-danger small">
        {{ tagCreationError }}
      </div>
      <!-- privacy selection -->
      <div class="flex align-items-center gap-3">
        <span>Privacy:</span>
        <ToggleButton
          v-model="newPrivacy"
          onLabel="Public"
          onIcon="pi pi-globe"
          offLabel="Private"
          offIcon="pi pi-lock"
          class="w-36"
          aria-label="Do you confirm"
        />
        <span class="font-italic">*Visibility on gallery page*</span>
      </div>
    </div>
    <template #footer>
      <Button
        label="Cancel"
        text
        severity="secondary"
        @click="visible = false"
        autofocus
      />
      <Button
        :label="isEditing ? 'Save' : 'Upload'"
        severity="secondary"
        :disabled="loading || tagCreationLoading"
        @click="upload"
        autofocus
      />
    </template>
  </Dialog>
</template>

<script setup lang="ts">
import Button from "primevue/button";
import Dialog from "primevue/dialog";
import InputText from "primevue/inputtext";
import { computed, ref, watch } from "vue";
import Art from "@/entities/Art";
import ToggleButton from "primevue/togglebutton";
import ArtAccessService from "@/services/ArtAccessService";
import { useToast } from "primevue/usetoast";
import router from "@/router";
import LoginService from "@/services/LoginService";
import { useLayerStore } from "@/store/LayerStore";
import { useArtistStore } from "@/store/ArtistStore";
import TagService from "@/services/TagService";
import MultiSelect from "primevue/multiselect";

const layerStore = useLayerStore();
const artistStore = useArtistStore();
const toast = useToast();
const visible = ref<boolean>(false);
const loading = ref<boolean>(false);
const isHover = ref<boolean>(false);

const newName = ref<string>("");
const newPrivacy = ref<boolean>(false);

const allTags = ref<any[]>([]);
const selectedTagIds = ref<number[]>([]);
const newTagName = ref<string>("");

const tagCreationError = ref<string>("");
const tagCreationLoading = ref<boolean>(false);

// --- Simplified tag creation (remove queue) ---
let creatingTagsPromise: Promise<void> | null = null;

const TAG_MAX_LENGTH = 15;
function sanitizeTagInput(raw: string): string {
  return raw
    .replace(/[^a-zA-Z0-9]/g, "")
    .toLowerCase()
    .slice(0, TAG_MAX_LENGTH);
}

function extractTokens(raw: string): string[] {
  return raw
    .split(/[,\s]+/)
    .map(sanitizeTagInput)
    .filter((t) => t.length > 0);
}

watch(newTagName, (val) => {
  if (!val) return;
  const rebuilt = val
    .split(/[,\s]+/)
    .map((v) => sanitizeTagInput(v))
    .filter(Boolean)
    .join(" ");
  if (rebuilt !== val.trim()) newTagName.value = rebuilt;
});

async function createTag(): Promise<void> {
  tagCreationError.value = "";
  const tokens = extractTokens(newTagName.value || "");
  newTagName.value = "";
  if (!tokens.length) {
    tagCreationError.value = "Enter a tag.";
    return;
  }

  // Build list of tokens that are not already present
  const toCreate = tokens.filter((token) => {
    const exists = allTags.value.some((t) => t.name.toLowerCase() === token);
    return !exists;
  });

  if (!toCreate.length) {
    // If all already exist, auto-select any up to limit
    tokens.forEach((tok) => {
      const tag = allTags.value.find((t) => t.name.toLowerCase() === tok);
      if (
        tag &&
        selectedTagIds.value.length < 4 &&
        !selectedTagIds.value.includes(Number(tag.id))
      ) {
        selectedTagIds.value.push(Number(tag.id));
      }
    });
    onTagChange();
    return;
  }

  tagCreationLoading.value = true;
  creatingTagsPromise = (async () => {
    const errors: string[] = [];
    await Promise.all(
      toCreate.map(async (token) => {
        try {
          const newTag = await TagService.createTag(token);
          newTag.id = Number(newTag.id);
          allTags.value.push(newTag);
          if (selectedTagIds.value.length < 4) {
            selectedTagIds.value.push(newTag.id);
          }
        } catch (e) {
          console.error("Failed creating tag:", token, e);
          errors.push(token);
        }
      })
    );
    if (errors.length) {
      tagCreationError.value = `Failed: ${errors.join(", ")}`;
    }
    tagCreationLoading.value = false;
    creatingTagsPromise = null;
    onTagChange();
  })();
  await creatingTagsPromise; // keep button responsive only after finish
}

const props = defineProps<{
  art: Art;
  fps: number;
}>();

const isEditing = computed(() => {
  return props.art.id != 0;
});

const emit = defineEmits(["openModal", "disconnect"]);

watch(visible, () => {
  emit("openModal", visible.value);
});

// function toggleModal(): void { //OLD toggleModal();
//   visible.value = !visible.value;
//   newName.value = props.art.title;
//   if (newName.value == "") {
//     newName.value = "Untitled";
//   }
//   newPrivacy.value = props.art.isPublic;
// }

const filterQuery = ref<string>("");

const displayedTags = computed(() => {
  const src = allTags.value || [];
  const q = filterQuery.value.trim().toLowerCase();

  // No query: stable alphabetical
  if (!q) {
    return [...src].sort((a, b) => a.name.localeCompare(b.name));
  }

  // Score matches but DO NOT remove non‑matches; just push them after matches
  const scored = src.map((t) => {
    const name = t.name?.toLowerCase() || "";
    let score = 0;
    if (name === q) score = 5;
    else if (name.startsWith(q)) score = 4;
    else if (name.includes(q)) score = 3;
    else {
      const overlap = [...new Set(q)].filter((ch) => name.includes(ch)).length;
      if (overlap) score = 1;
    }
    return { ref: t, score };
  });

  return scored
    .sort((a, b) => b.score - a.score || a.ref.name.localeCompare(b.ref.name))
    .map((x) => x.ref);
});

function onTagFilter(e: any) {
  filterQuery.value = e.value || "";
}

function onTagChange() {
  if (selectedTagIds.value.length > 4) {
    // Clamp to 4 if somehow exceeded (race or manual mutation)
    selectedTagIds.value = selectedTagIds.value.slice(0, 4);
    toast.add({
      severity: "warn",
      summary: "Limit Reached",
      detail: "You can select up to 4 tags.",
      life: 2000
    });
  }
}

async function toggleModal(): Promise<void> {
  visible.value = !visible.value;
  newName.value = props.art.title;
  if (newName.value == "") {
    newName.value = "Untitled";
  }
  newPrivacy.value = props.art.isPublic;

  if (visible.value) {
    try {
      allTags.value = await TagService.getAllTags();
      allTags.value = allTags.value.map((t) => ({ ...t, id: Number(t.id) }));
      if (isEditing.value && props.art.id) {
        try {
          const existing = await TagService.getTagsForArt(props.art.id);
          selectedTagIds.value = existing
            .map((t: any) => Number(t.id))
            .filter((v, i, arr) => arr.indexOf(v) === i)
            .slice(0, 4);
        } catch (e) {
          console.warn("Failed to load existing tags for art", e);
          selectedTagIds.value = [];
        }
      } else {
        selectedTagIds.value = [];
      }
    } catch (e) {
      console.error("Failed loading tags", e);
      allTags.value = [];
      selectedTagIds.value = [];
    }
  }
}

function flattenArtEncode(): string {
  let width = layerStore.grids[0].width;
  let height = layerStore.grids[0].height;
  let arr: string[][] = Array.from({ length: height }, () =>
    Array(width).fill(layerStore.grids[0].backgroundColor.toLowerCase())
  );

  for (let length = 0; length < layerStore.grids.length; length++) {
    for (let i = 0; i < height; i++) {
      for (let j = 0; j < width; j++) {
        //only set empty cells to background color if its the first layer
        //layers above the first will just replace cells if they have a value
        if (layerStore.grids[length].grid[i][j] !== "empty") {
          arr[i][j] = layerStore.grids[length].grid[i][j];
        }
      }
    }
  }
  return arr.flat().join("");
}
function FlattenFrameEncode(index: number): string {
  let width = layerStore.grids[index].width;
  let height = layerStore.grids[index].height;
  let arr: string[][] = Array.from({ length: height }, () =>
    Array(width).fill(layerStore.grids[0].backgroundColor)
  );

  for (let i = 0; i < height; i++) {
    for (let j = 0; j < width; j++) {
      if (layerStore.grids[index].grid[i][j] !== "empty") {
        arr[i][j] = layerStore.grids[index].grid[i][j];
      }
    }
  }
  return arr.flat().join("");
}
function handleNotLoggedIn(): void {
  toast.add({
    severity: "error",
    summary: "Error",
    detail: "You must be logged in to upload art",
    life: 3000
  });
  loading.value = false;
  visible.value = false;
}

async function finalizeUpload(success: boolean, artId?: number): Promise<void> {
  loading.value = false;
  visible.value = false;

  if (success && artId) {
    // Removed re-fetch & filtering of newly created tag ids to avoid race
    let tagError = false;
    const ids = selectedTagIds.value
      .map((id) => Number(id))
      .filter((id) => Number.isFinite(id) && id > 0);

    if (ids.length) {
      try {
        await TagService.assignTagsToArt(artId, ids);
      } catch (e) {
        console.error("Failed to assign tags:", e);
        tagError = true;
      }
    }

    toast.add({
      severity: tagError ? "warn" : "success",
      summary: tagError
        ? "Partial Success"
        : isEditing.value
        ? "Saved"
        : "Uploaded",
      detail: tagError
        ? "Art saved, but some tags failed. You can edit and re-apply."
        : "Art uploaded successfully",
      life: 3500
    });

    console.debug("Navigating to art detail:", artId);
    layerStore.empty();
    artistStore.empty();
    try {
      await router.push("/art/" + artId);
      // Fallback if route name differs or push silently ignored
      if (!router.currentRoute.value.path.endsWith("/" + artId)) {
        console.warn(
          "Primary router.push did not change route, retrying replace"
        );
        await router.replace("/art/" + artId);
      }
    } catch (navErr) {
      console.error("Navigation failed, forcing location change:", navErr);
      window.location.href = "/art/" + artId;
    }
  } else if (!success) {
    toast.add({
      severity: "error",
      summary: "Error",
      detail: "Failed to upload art",
      life: 3000
    });
  }
}
async function upload(): Promise<void> {
  // Wait for any in-progress tag creations
  if (creatingTagsPromise) {
    await creatingTagsPromise;
  }

  emit("disconnect");
  loading.value = true;

  const isLoggedIn = await LoginService.isLoggedIn();
  if (!isLoggedIn) {
    handleNotLoggedIn();
    return;
  }

  if (props.art.isGif) {
    const paintings: Art[] = layerStore.grids.map((grid, i) => {
      const newArt = new Art();
      newArt.title = newName.value;
      newArt.isPublic = newPrivacy.value;
      newArt.pixelGrid.deepCopy(grid);
      newArt.id = props.art.id;
      newArt.gifFrameNum = i + 1;
      newArt.isGif = true;
      newArt.pixelGrid.encodedGrid = FlattenFrameEncode(i);
      newArt.artistId = props.art.artistId;
      newArt.artistName = props.art.artistName;
      newArt.gifFps = props.fps;
      return newArt;
    });

    try {
      const savedGif = await ArtAccessService.saveGif(paintings);
      console.debug("saveGif result:", savedGif);
      const newId = Array.isArray(savedGif) ? savedGif[0]?.id : savedGif?.id;
      await finalizeUpload(!!newId, newId);
    } catch (error) {
      console.error("Upload (saveGif) failed", error);
      await finalizeUpload(false);
    }
  } else {
    const newArt = new Art();
    newArt.title = newName.value;
    newArt.isPublic = newPrivacy.value;
    newArt.pixelGrid.deepCopy(layerStore.grids[0]);
    newArt.id = props.art.id;
    newArt.pixelGrid.encodedGrid = flattenArtEncode();
    newArt.artistId = props.art.artistId;
    newArt.artistName = props.art.artistName;

    try {
      const saved = await ArtAccessService.saveArt(newArt);
      console.debug("saveArt result:", saved);
      await finalizeUpload(!!saved?.id, saved?.id);
    } catch (error) {
      console.error("Upload (saveArt) failed", error);
      await finalizeUpload(false);
    }
  }
}
</script>
