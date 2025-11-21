<template>
  <div class="gallery-container mx-auto my-0">
    <header class="gallery-header">
      <h1 class="page-title">Search for Art</h1>

      <!-- Top row (medium+ screens): Title | Artist | Order Toggle | Date/Sort dropdown -->
      <div class="search-row">
        <InputText
          v-model.trim="search"
          type="text"
          placeholder="Search title..."
          class="input title-input"
        />
        <InputText
          v-model.trim="filter"
          type="text"
          placeholder="Search artists..."
          class="input artist-input"
        />
        <!-- Moved order toggle into top row (left of dropdown) -->
        <ToggleButton
          v-if="isSortedByDate"
          class="order-toggle"
          v-model="checkAscending"
          onLabel="Oldest First"
          onIcon="pi pi-arrow-up"
          offLabel="Newest First"
          offIcon="pi pi-arrow-down"
          @click="sortGallery()"
        />
        <ToggleButton
          v-else
          class="order-toggle"
          v-model="checkAscending"
          onLabel="Ascending"
          onIcon="pi pi-arrow-up"
          offLabel="Descending"
          offIcon="pi pi-arrow-down"
          @click="sortGallery()"
        />
        <Dropdown
          class="input sort-dropdown"
          v-model="sortType"
          :options="sortBy"
          optionLabel="sort"
          optionValue="code"
          placeholder="Date / Sort"
        />
      </div>

      <!-- Row 2 now only: per page selector -->
      <div class="controls-row">
        <div class="per-page-group">
          <label class="density-label" for="perPageSelect">Art per page:</label>
          <Dropdown
            id="perPageSelect"
            class="control per-page-select"
            v-model="perPage"
            :options="paginationOptions"
          />
        </div>
      </div>

      <!-- Row 3: combined Tags + Locations (was two rows) -->
      <div class="filter-row">
        <span class="tags-label">Tags</span>
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
        <ToggleButton
          class="match-toggle"
          v-model="matchAll"
          :title="matchLabel"
          :aria-label="matchLabel"
          onLabel="Match All"
          offLabel="Match Any"
          onIcon="pi pi-check-circle"
          offIcon="pi pi-circle"
        />
        <Button
          class="clear-tags-btn"
          icon="pi pi-times"
          label="Clear Tags"
          title="Clear selected tags"
          aria-label="Clear selected tags"
          severity="secondary"
          outlined
          @click="clearSelectedTags"
        />

        <!-- Locations immediately after tags -->
        <span class="locations-label">Locations</span>
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
  return selectedTagIds.value
    .map(id => map.get(Number(id)))
    .filter((n): n is string => !!n);
});

// Names of selected locations for the banner
const selectedLocationNames = computed(() => {
  const map = new Map<number, string>(
    (allLocations.value || []).map(t => [Number(t.id), String(t.title)])
  );
  console.log(map);
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
  return "";
});

// Helper: recompute displayArt based on tags, search, and artist filter
function updateDisplay(): void {
  let list = publicArt.value;

  // Tag filtering (MultiSelect takes precedence; fallback to route tag)
  if (selectedTagIds.value.length > 0) {
    const wanted = selectedTagIds.value.map(Number);
    const requireAll = matchAll.value;
    list = list.filter(a => {
      const ids = (a.tags || []).map(t => Number(t.id)).filter(Number.isFinite);
      if (!ids.length) return false;
      return requireAll
        ? wanted.every(id => ids.includes(id))
        : wanted.some(id => ids.includes(id));
    });
  } else if (activeTag.value) {
    const tagLower = activeTag.value.toLowerCase();
    list = list.filter(a =>
      (a.tags || []).some(t => (t.name || "").toLowerCase() === tagLower)
    );
  }

  if (filter.value) {
    list = list.filter(a =>
      a.artistName.toString().toLowerCase().includes(filter.value.toLowerCase())
    );
  }

  if (search.value) {
    list = list.filter(a =>
      a.title.toLowerCase().includes(search.value.toLowerCase())
    );
  }

  displayArt.value = list.slice();
  isModified.value = true;

  const maxPage = Math.max(1, Math.ceil(displayArt.value.length / perPage.value));
  if (currentPage.value > maxPage) currentPage.value = 1;
}

onMounted(async () => {

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

  // If we arrived via /tag/:tag, reflect that in the multi-select when possible
  if (activeTag.value && allTags.value.length) {
    const found = allTags.value.find(
      t => String(t.name).toLowerCase() === activeTag.value!.toLowerCase()
    );
    if (found) selectedTagIds.value = [Number(found.id)];
  }

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
/* Global fixes for smooth scrolling and no accidental horizontal scroll */
:global(html) { scroll-behavior: smooth; }
.gallery-container {
  width: 100%;
  max-width: 1200px;
  padding: 0 1rem;
  box-sizing: border-box;
  overflow-x: hidden; /* no horizontal scroll */
}

/* Header */
.gallery-header { display: block; margin-bottom: .75rem; }
.page-title { font-size: 1.4rem; font-weight: 800; margin: .25rem 0 .5rem; padding-top: 10px;}

/* Top search row with toggle + dropdown */
.search-row {
  display: grid;
  gap: .5rem;
  margin-bottom: .6rem;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
}

.order-toggle {
  min-width: 140px;             /* adjust toggle width */
  justify-self: start;
}

.sort-dropdown {
  min-width: 160px;              /* adjust min width of date/sort dropdown */
  max-width: 220px;
  justify-self: end;
}

.title-input,
.artist-input { min-width: 180px; }

/* Medium+ screens: fixed 4-column layout (Title | Artist | Toggle | Dropdown) */
@media (min-width: 900px) {
  .search-row {
    grid-template-columns:
      minmax(220px, 1fr)
      minmax(220px, 1fr)
      minmax(140px, 180px)
      minmax(130px, 100px);
  }
}

/* Mobile: stack; toggle precedes dropdown */
@media (max-width: 899px) {
  .search-row { grid-template-columns: 1fr; }
  .order-toggle,
  .sort-dropdown { justify-self: stretch; }
  .sort-dropdown { max-width: 100%; }
}

/* Removed toggle buttons from controls-row; adjust spacing */
.controls-row {
  display: flex;
  flex-wrap: wrap;
  gap: .75rem;
  align-items: center;
  margin-bottom: .6rem;
}

/* Small/mobile: stack everything (dropdown goes under inputs) */
@media (max-width: 899px) {
  .search-row {
    grid-template-columns: 1fr;
  }
  .sort-dropdown {
    justify-self: stretch;
    max-width: 100%;
  }
}

/* Sort + order controls (wrap when needed) */
.controls-row {
  display: flex;
  flex-wrap: wrap;
  gap: .75rem;
  align-items: center;
  margin-bottom: .6rem;
}
.controls-row .control { min-width: 10rem; }

.per-page-group {
  display: inline-flex;
  align-items: center;
  gap: .4rem;
  flex-wrap: wrap;
}
.per-page-select { min-width: 6.5rem; }

/* Combined filter row (Tags + Locations) */
/* COMMENT: adjust gap / margin-bottom to tune spacing between filter row and feed */
.filter-row {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: .6rem;
  margin-bottom: .5rem; /* ← adjust vertical space below combined row */
}

.tags-label,
.locations-label {
  font-weight: 700;
  margin-right: .25rem;
}

.match-toggle { margin-left: .25rem; }

:deep(.tag-multiselect.p-multiselect),
:deep(.location-multiselect.p-multiselect) {
  width: auto;
  min-width: 16rem; /* ← adjust min width of selects */
  max-width: 40rem; /* ← adjust max width of selects */
}

@media (max-width: 900px) {
  .filter-row {
    flex-direction: column;
    align-items: stretch;
  }
  .match-toggle,
  .clear-tags-btn {
    align-self: flex-start;
  }
  :deep(.tag-multiselect.p-multiselect),
  :deep(.location-multiselect.p-multiselect) {
    width: 100%;
    min-width: 0;
    max-width: none;
  }
}

/* Remove old individual row styles (tag-filter-row, location-row, density-row) now unused */
.tag-filter-row,
.location-row,
.density-row { display: none !important; }

/* ART LAYOUT: rows/columns on medium–large screens, feed only on small mobile */
.gallery-feed {
  --art-card-min: 260px;         /* adjust min card width if needed */
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(var(--art-card-min), 1fr));
  gap: 1rem;
  width: 100%;
  max-width: 1200px;             /* center the grid like Account page */
  margin-left: auto;
  margin-right: auto;
  box-sizing: border-box;
  overflow-x: hidden;
}
.feed-item { min-width: 0; }

/* Force exactly 4 columns on desktop to avoid 4→5 jump jitter */
@media (min-width: 1001px) {
  .gallery-feed {
    grid-template-columns: repeat(4, minmax(0, 1fr)) !important;
  }
}

/* Mobile: single-column scrolling feed */
@media (max-width: 1000px) {
  .gallery-feed {
    display: flex !important;
    flex-direction: column;
    gap: 0.75rem;
    max-width: 100%;
  }
  .feed-item { width: 100%; }
}

/* keep media responsive inside cards */
.gallery-feed :deep(img),
.gallery-feed :deep(canvas),
.gallery-feed :deep(video) {
  max-width: 100%;
  height: auto;
  display: block;
}
</style>

<!-- 10 -->