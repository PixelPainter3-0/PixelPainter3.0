<template>
  <div class="gallery-container mx-auto my-0">
    <header class="gallery-header">
      <h1 class="page-title">Search for Art</h1>

      <!-- Top search row: title + artist side by side -->
      <div class="search-row">
        <InputText
          v-model.trim="search"
          type="text"
          placeholder="Search title..."
          class="input"
        />
        <InputText
          v-model.trim="filter"
          type="text"
          placeholder="Search artists..."
          class="input"
        />
      </div>

      <!-- Sort and order controls (visible, wrapping when needed) -->
      <div class="controls-row">
        <Dropdown
          class="control"
          v-model="sortType"
          :options="sortBy"
          optionLabel="sort"
          optionValue="code"
          placeholder="Sort by"
        />

        <ToggleButton
          v-if="isSortedByDate"
          class="control"
          v-model="checkAscending"
          onLabel="Oldest First"
          onIcon="pi pi-arrow-up"
          offLabel="Newest First"
          offIcon="pi pi-arrow-down"
          @click="sortGallery()"
        />
        <ToggleButton
          v-else
          class="control"
          v-model="checkAscending"
          onLabel="Ascending"
          onIcon="pi pi-arrow-up"
          offLabel="Descending"
          offIcon="pi pi-arrow-down"
          @click="sortGallery()"
        />
      </div>

      <!-- Tag multi-select row (kept visible, wraps nicely on mobile) -->
      <div class="tag-filter-row">
        <span class="mr-2 tags-label">Tags</span>
        <MultiSelect
          class="tag-multiselect"
          v-model="selectedTagIds"
          :options="displayedTags"
          optionLabel="name"
          optionValue="id"
          display="chip"
          filter
          placeholder="Filter by tag(s)"
          :maxSelectedLabels="4"
          @filter="onTagFilter"
        />
        <MultiSelect
          class="location-multiselect"
          v-model="selectedLocationIds"
          :options="displayedLocations"
          optionLabel="title"
          optionValue="id"
          display="chip"
          filter
          placeholder="Filter by location(s)"
          :maxSelectedLabels="4"
          @filter="onTagFilter"
        />
        <ToggleButton
          class="ml-2"
          v-model="matchAll"
          :title="matchLabel"
          :aria-label="matchLabel"
          onLabel="Match All"
          offLabel="Match Any"
          onIcon="pi pi-check-circle"
          offIcon="pi pi-circle"
        />
        <Button
          class="ml-2 clear-tags-btn"
          icon="pi pi-times"
          label="Clear Tags"
          title="Clear selected tags"
          aria-label="Clear selected tags"
          severity="secondary"
          outlined
          @click="clearSelectedTags"
        />
      </div>

      <!-- Location select: always placed below the Tags row -->
      <div class="location-row">
        <span class="mr-2 tags-label">Locations</span>
        <MultiSelect
          class="location-multiselect"
          v-model="selectedLocationIds"
          :options="displayedLocations"
          optionLabel="title"
          optionValue="id"
          display="chip"
          filter
          placeholder="Filter by location(s)"
          :maxSelectedLabels="4"
          @filter="onTagFilter"
        />
      </div>

      <!-- Centered banner -->
      <div v-if="bannerText" class="tag-banner">
        Showing {{ bannerText }} Art
      </div>

      <div class="density-row">
        <label class="density-label">Art per page:</label>
        <Dropdown
          class="control"
          v-model="perPage"
          :options="paginationOptions"
        />
      </div>
    </header>

    <!-- Feed: single column on mobile, multi on larger screens -->
    <div class="gallery-feed" v-if="!loading && displayArt.length > 0">
      <div
        v-for="index in displayAmount"
        :key="index"
        class="feed-item"
      >
        <ArtCard
          :art="displayArt[index + offset]"
          :size="10"
          :position="index"
        />
      </div>
    </div>

    <!-- No results -->
    <div v-if="!loading && displayArt.length === 0" class="no-results">
      <i class="pi pi-search" aria-hidden="true"></i>
      <p>No art found for the current filters.</p>
      <small v-if="selectedTagIds.length > 0">Try switching to Match Any or clear selected tags.</small>
    </div>

    <ArtPaginator v-if="pages > 1" :pages="pages" @page-change="changePage" />
  </div>
</template>

<script setup lang="ts">
import { onMounted, ref, watch, computed } from "vue";
import { useRoute } from "vue-router";
import router from "@/router"; // add this
import ArtCard from "@/components/Gallery/ArtCard.vue";
import Art from "@/entities/Art";
import Point from "@/entities/Point";
import ArtAccessService from "@/services/ArtAccessService";
import MapAccessService from "../services/MapAccessService";
import InputText from "primevue/inputtext";
import Dropdown from "primevue/dropdown";
import ToggleButton from "primevue/togglebutton";
import ArtPaginator from "@/components/Gallery/ArtPaginator.vue";
import MultiSelect from "primevue/multiselect";
import TagService from "@/services/TagService";
import Button from "primevue/button"; // ADD THIS

const publicArt = ref<Art[]>([]);
const displayArt = ref<Art[]>([]);
const search = ref<string>("");
const filter = ref<string>("");
const loading = ref<boolean>(true);

// Route tag (used to seed the multi-select)
const route = useRoute();
const activeTag = ref<string | null>((route.params.tag as string) || null);
const activeLocation = Number(route.params.location);

// Sorting / paging
interface sortFilter {
  sort: string;
  code: string;
}
const sortBy = ref<sortFilter[]>([
  { sort: "Likes", code: "L" },
  { sort: "Comments", code: "C" },
  { sort: "Date", code: "D" }
]);
const paginationOptions = ref<Number[]>([12, 24, 36]);
const sortType = ref<string>("D");
const isSortedByDate = ref<boolean>(true);
const checkAscending = ref<boolean>(false);
const isModified = ref<boolean>(false);
const currentPage = ref<number>(1);
const perPage = ref<number>(12);
const flicker = ref<boolean>(true);
const pages = computed(() => Math.ceil(displayArt.value.length / perPage.value));
const displayAmount = computed(() => {
  const total = displayArt.value.length;
  if (total === 0) return 0; // prevent rendering when no matches
  if (currentPage.value === pages.value) {
    const rem = total % perPage.value;
    return rem === 0 ? perPage.value : rem;
  }
  return perPage.value;
});
const offset = computed(() => perPage.value * (currentPage.value - 1) - 1);
watch(perPage, () => {
  flicker.value = false;
  changePage(1);
  flicker.value = true;
});

// NEW: tag data for multi-select
const allTags = ref<any[]>([]);
const allLocations = ref<any[]>([]);
const selectedTagIds = ref<number[]>([]);
const selectedLocationIds = ref<number[]>([]);
const filterQuery = ref<string>("");
const matchAll = ref<boolean>(false);
const matchLabel = computed(() => (matchAll.value ? "Match All" : "Match Any"));

const displayedTags = computed(() => {
  const src = allTags.value || [];
  const q = filterQuery.value.trim().toLowerCase();

  if (!q) return [...src].sort((a, b) => a.name.localeCompare(b.name));

  const scored = src.map(t => {
    const name = t.name?.toLowerCase() || "";
    let score = 0;
    if (name === q) score = 5;
    else if (name.startsWith(q)) score = 4;
    else if (name.includes(q)) score = 3;
    else {
      const overlap = [...new Set(q)].filter(ch => name.includes(ch)).length;
      if (overlap) score = 1;
    }
    return { ref: t, score };
  });

  return scored
    .sort((a, b) => b.score - a.score || a.ref.name.localeCompare(b.ref.name))
    .map(x => x.ref);
});

const displayedLocations = computed(() => {
  const src = allLocations.value || [];
  const q = filterQuery.value.trim().toLowerCase();

  if (!q) return [...src].sort((a, b) => a.title.localeCompare(b.title));

  const scored = src.map(t => {
      const title = t.title?.toLowerCase() || "";
    let score = 0;
      if (title === q) score = 5;
      else if (title.startsWith(q)) score = 4;
      else if (title.includes(q)) score = 3;
    else {
          const overlap = [...new Set(q)].filter(ch => title.includes(ch)).length;
      if (overlap) score = 1;
    }
    return { ref: t, score };
  });

  return scored
    .sort((a, b) => b.score - a.score || a.ref.title.localeCompare(b.ref.title))
    .map(x => x.ref);
});

function onTagFilter(e: any) {
  filterQuery.value = e.value || "";
}

function goToBaseGallery(): void {
  // If we're on /tag/:tag, go to the main gallery
  const onTagRoute = String(route.path || "").startsWith("/tag/");
  if (onTagRoute) {
    router.replace("/gallery").catch(() => {});
  }
  activeTag.value = null;
}

function clearSelectedTags(): void {
  selectedTagIds.value = [];
  selectedLocationIds.value = [];
  goToBaseGallery();
  changePage(1);
  updateDisplay();
}

// Names of selected tags for the banner
const selectedTagNames = computed(() => {
  const map = new Map<number, string>(
    (allTags.value || []).map(t => [Number(t.id), String(t.name)])
  );
  console.log("tagMap", map);
  return selectedTagIds.value
    .map(id => map.get(Number(id)))
    .filter((n): n is string => !!n);
});

// Names of selected locations for the banner
const selectedLocationNames = computed(() => {
  const map = new Map<number, string>(
    (allLocations.value || []).map(t => [Number(t.id), String(t.title)])
  );
  console.log("locationMap", map);
  return selectedLocationIds.value
    .map(id => map.get(Number(id)))
    .filter((n): n is string => !!n);
});

const bannerText = computed(() => {
  if (selectedTagNames.value.length) {
    return `'${selectedTagNames.value.join("', '")}'`;
  }
  if (activeTag.value) {
    return `'${activeTag.value}'`;
  }
  if (selectedLocationNames.value.length) {
    return `'${selectedLocationNames.value.join("', '")}'`;
  }
  return "";
});

// Helper: recompute displayArt based on tags, search, and artist filter
function updateDisplay(): void {
  let list = publicArt.value;
  // Tag filtering (MultiSelect takes precedence; fallback to route tag)
  if (selectedTagIds.value.length > 0) {
    const wanted = selectedTagIds.value.map(Number);
    const requireAll = matchAll.value;

    console.log(`[Tags] Filtering by selected IDs: [${wanted.join(', ')}]`)

    list = list.filter(a => {
      const ids = (a.tags || []).map(t => Number(t.id)).filter(Number.isFinite);
      if (!ids.length) return false;
      return requireAll
        ? wanted.every(id => ids.includes(id))
        : wanted.some(id => ids.includes(id));
    });
  } else if (selectedLocationIds.value.length > 0) {
    const wanted = selectedLocationIds.value.map(Number);
    const requireAll = matchAll.value;

    console.log(`[Locations] Filtering by selected IDs: [${wanted.join(', ')}]`)
    
    list = list.filter(a => {
      const locId = Number(a.pointId);
      if (!Number.isFinite(locId)) return false;
      return wanted.includes(locId);
    });
  } else if (activeTag.value) {
    const tagLower = activeTag.value.toLowerCase();
    list = list.filter(a =>
      (a.tags || []).some(t => (t.name || "").toLowerCase() === tagLower)
    );
  }

  // Artist name
  if (filter.value) {
    list = list.filter(a =>
      a.artistName.toString().toLowerCase().includes(filter.value.toLowerCase())
    );
  }

  console.log("list", list);
  displayArt.value = list.slice();
  isModified.value = true;

  const maxPage = Math.max(1, Math.ceil(displayArt.value.length / perPage.value));
  if (currentPage.value > maxPage) currentPage.value = 1;
}

onMounted(async () => {

  //console.log('MapAccessService:', MapAccessService);  


  //console.log('MapAccessService:', MapAccessService);  

  // Load tags for the multi-select
  try {
    allTags.value = await TagService.getAllTags();
    allTags.value = (allTags.value || []).map(t => ({ ...t, id: Number(t.id) }));
  } catch {
    allTags.value = [];
  }

  // Load locations for the multi-select
  try {
    allLocations.value = await MapAccessService.getAllPoints();
    allLocations.value = (allLocations.value || []).map(t => ({ ...t, id: Number(t.id) }));
    console.log('allLocations:', allLocations.value);
  } catch (err) {
    console.error('Error loading locations:', err);
    allLocations.value = [];
  }

  // Load locations for the multi-select
  try {
    allLocations.value = await MapAccessService.getAllPoints();
    allLocations.value = (allLocations.value || []).map(t => ({ ...t, id: Number(t.id) }));
    console.log('allLocations:', allLocations.value);
  } catch (err) {
    console.error('Error loading locations:', err);
    allLocations.value = [];
  }

  // If we arrived via /tag/:tag, reflect that in the multi-select when possible
  if (activeTag.value && allTags.value.length) {
    const found = allTags.value.find(
      t => String(t.name).toLowerCase() === activeTag.value!.toLowerCase()
    );
    if (found) selectedTagIds.value = [Number(found.id)];
  }

  if (activeLocation && allLocations.value.length) {
      const found = allLocations.value.find(
        t => Number(t.id) === activeLocation!
    );
    if (found) selectedLocationIds.value = [Number(found.id)];
  }

  console.log('activeTag: ', activeTag.value);
  console.log('activeLocation: ', activeLocation);

  // Load and show art
  ArtAccessService.getAllArt()
    .then((data) => {
      publicArt.value = data;
      sortGallery();
      updateDisplay();
    })
    .finally(() => {
      loading.value = false;
    });
});

// React to route tag changes (seed selection) 
watch(
  () => route.params.tag,
  (newTag) => {
    activeTag.value = newTag ? String(newTag) : null;

    if (!activeTag.value) {
      // If tag cleared, do not overwrite existing multi-select choices
      updateDisplay();
      return;
    }

    // Try to select the route tag if we know it
    const found = (allTags.value || []).find(
      t => String(t.name).toLowerCase() === activeTag.value!.toLowerCase()
    );
    if (found) selectedTagIds.value = [Number(found.id)];
    changePage(1);
    updateDisplay();
  }
);

// Recompute on input changes
watch(search, updateDisplay);
watch(filter, updateDisplay);
watch(sortType, () => { sortGallery(); });

// When tags or match mode change, clear the route tag if empty selection
watch([selectedTagIds, matchAll], ([ids]) => {
  if (!ids || (Array.isArray(ids) && ids.length === 0)) {
    goToBaseGallery();
  }
  changePage(1);
  updateDisplay();
});

// When location or match mode change, clear the route tag if empty selection
watch([selectedLocationIds, matchAll], ([ids]) => {
  if (!ids || (Array.isArray(ids) && ids.length === 0)) {
    goToBaseGallery();
  }
  changePage(1);
  updateDisplay();
});

function changePage(page: number): void {
  currentPage.value = page;
}

function sortGallery(): void {
  const sortCode = sortType.value;
  isModified.value = true;

  isSortedByDate.value = false;
  if (sortCode == "L") {
    // Sort By Likes
    if (checkAscending.value) {
      publicArt.value.sort((artA, artB) => artA.numLikes - artB.numLikes);
    } else {
      publicArt.value.sort((artA, artB) => artB.numLikes - artA.numLikes);
    }
  }
    // Sort By Comments
  else if (sortCode == "C") {
    if (checkAscending.value) {
      publicArt.value.sort((artA, artB) => artA.numComments - artB.numComments);
    } else {
      publicArt.value.sort((artA, artB) => artB.numComments - artA.numComments);
    }
  } else if (sortCode == "D") {
    isSortedByDate.value = true;
    if (checkAscending.value) {
      publicArt.value.sort(
        (artA, artB) =>
          new Date(artA.creationDate).getTime() -
          new Date(artB.creationDate).getTime()
      );
    } else {
      publicArt.value.sort(
        (artA, artB) =>
          new Date(artB.creationDate).getTime() -
          new Date(artA.creationDate).getTime()
      );
    }
  }
  updateDisplay();
}
</script>

<style scoped>
.tag-filter-row {
  display: flex;
  align-items: center;
  gap: .5rem;
  margin-top: .25rem;
  flex-wrap: wrap; /* allow row to wrap on small screens */
}

/* Input grows to fit selected chips (up to a max) */
:deep(.tag-multiselect.p-multiselect) {
  width: auto;           /* grow with content */
  min-width: 20rem;      /* keep a sensible base width */
  max-width: 48rem;      /* prevent overgrowing the row */
}

/* Allow selected chips to wrap to multiple lines inside the control */
:deep(.tag-multiselect .p-multiselect-label) {
  white-space: normal;
  display: flex;
  flex-wrap: wrap;
  gap: .25rem;
}

/* Make the dropdown panel taller so more tag options are visible */
:deep(.p-multiselect-panel .p-multiselect-items) {
  max-height: 60vh;      /* show many tags, still constrained to viewport */
  overflow: auto;
}

.tag-banner {
  width: 100%;
  max-width: 1200px;
  padding: 0 1rem;
  box-sizing: border-box;
  overflow-x: hidden; /* no horizontal scroll */
}

/* Header */
.gallery-header { display: block; margin-bottom: .75rem; }
.page-title { font-size: 1.4rem; font-weight: 800; margin: .25rem 0 .5rem; padding-top: 10px;}

/* Top search row (side-by-side on mobile) */
.search-row {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: .5rem;
  align-items: center;
  margin-bottom: .5rem;
}
.search-row .input { width: 100%; min-width: 0; }

/* Sort + order controls (wrap when needed) */
.controls-row { display: flex; flex-wrap: wrap; gap: .5rem; align-items: center; margin-bottom: .5rem; }
.controls-row .control { min-width: 10rem; }

/* Tag row (already wraps) */
.tag-filter-row { display: flex; align-items: center; gap: .5rem; margin-top: .25rem; flex-wrap: wrap; }

:deep(.tag-multiselect.p-multiselect) { width: auto; min-width: 20rem; max-width: 48rem; }
:deep(.tag-multiselect .p-multiselect-label) { white-space: normal; display: flex; flex-wrap: wrap; gap: .25rem; }
:deep(.p-multiselect-panel .p-multiselect-items) { max-height: 60vh; overflow: auto; }

.tag-banner { width: 100%; text-align: center; margin-top: .25rem; margin-bottom: .5rem; font-size: 1.15rem; font-weight: 700; }

/* Per-page density row */
.density-row { display: flex; align-items: center; gap: .5rem; flex-wrap: wrap; margin: .25rem 0 .75rem; }
.density-label { font-weight: 600; }

/* make the "Tags" label bold */
.tags-label {
  font-weight: 700;
}

/* FEED LAYOUT (matches AccountPage art-grid behavior) */
.gallery-feed {
  --art-card-min: 260px;             /* adjust min card width here */
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(var(--art-card-min), 1fr));
  grid-auto-rows: auto;
  gap: 1rem;
  width: 100%;
  max-width: 1200px;                 /* keep in sync with AccountPage */
  margin-left: auto;
  margin-right: auto;
  box-sizing: border-box;
  overflow-x: hidden;                /* no horizontal scroll */
}
.feed-item { min-width: 0; }

/* Force exactly 4 columns on desktop to avoid 4→5 jump near ~1202px */
@media (min-width: 1001px) {
  .gallery-feed {
    grid-template-columns: repeat(4, minmax(0, 1fr)) !important;
    grid-auto-flow: row;
  }
}

/* Tablet: slightly smaller fixed card width if needed */
@media (min-width: 768px) and (max-width: 1023px) {
  .gallery-feed { --art-card-min: 220px; gap: 0.9rem; }
}

/* Mobile: full-width single column feed */
@media (max-width: 780px) {
   :deep(.tag-multiselect.p-multiselect) { width: 100%; min-width: 0; max-width: none; }

  .gallery-feed {
    display: flex !important;
    flex-direction: column;
    gap: 1rem;
    max-width: 100%;
  }
  .feed-item { width: 100%; }
}

/* ensure location row sits below tags and wraps nicely */
.location-row {
  display: flex;
  gap: .5rem;
  align-items: center;
  flex-wrap: wrap;
  margin-top: .5rem; /* adjust vertical spacing as needed */
}

/* keep the multiselect sizing behavior consistent with tags */
:deep(.location-multiselect.p-multiselect) { width: auto; min-width: 20rem; max-width: 48rem; }

/* On very small screens make both selects full width */
@media (max-width: 780px) {
  :deep(.tag-multiselect.p-multiselect),
  :deep(.location-multiselect.p-multiselect) { width: 100%; min-width: 0; max-width: none; }
}

/* Larger screens can breathe more */
@media (min-width: 1024px) {
  .page-title { font-size: 1.8rem; }
}
</style>

<!-- 6 -->