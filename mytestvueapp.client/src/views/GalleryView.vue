<template>
  <div class="gallery-container mx-auto my-0">
    <header class="gallery-header">
      <h1 class="page-title">Search for Art</h1>

      <!-- ROW 1 Desktop: Title | Artist | Date | Newest First -->
      <!-- Smoothly condenses: inputs grow, controls keep min width, wrap gracefully -->
      <div class="row row-top">
        <InputText
          v-model.trim="search"
          type="text"
          placeholder="Search title..."
          class="grow input title-input"
        />
        <InputText
          v-model.trim="filter"
          type="text"
          placeholder="Search artists..."
          class="grow input artist-input"
        />
        <Dropdown
          class="control date-dropdown"
          v-model="sortType"
          :options="sortBy"
          optionLabel="sort"
          optionValue="code"
          placeholder="Date"
        />
        <ToggleButton
          v-if="isSortedByDate"
          class="control newest-toggle"
          v-model="checkAscending"
          onLabel="Oldest First"
          onIcon="pi pi-arrow-up"
          offLabel="Newest First"
          offIcon="pi pi-arrow-down"
          @click="sortGallery()"
        />
        <ToggleButton
          v-else
          class="control newest-toggle"
          v-model="checkAscending"
          onLabel="Ascending"
          onIcon="pi pi-arrow-up"
          offLabel="Descending"
          offIcon="pi pi-arrow-down"
          @click="sortGallery()"
        />
      </div>

      <!-- ROW 2 Desktop: Tags | Match/Clear | Locations | Art per page -->
      <div class="row row-filters">
        <!-- Tags group -->
        <div class="filter-group filter-tags">
          <label class="group-label" for="tagsInput" id="tagsLabel">Tags</label>
          <MultiSelect
            id="tagsSelect"
            class="tag-multiselect filter-select"
            v-model="selectedTagIds"
            :options="displayedTags"
            optionLabel="name"
            optionValue="id"
            display="chip"
            filter
            placeholder="Filter by tag(s)"
            :maxSelectedLabels="4"
            inputId="tagsInput"
            aria-labelledby="tagsLabel"
            @filter="onTagFilter"
          />
        </div>
        <!-- Match/Clear grouped to stay together -->
        <div class="filter-inline">
        <div>
        <ToggleButton
              class="control match-toggle"
              v-model="matchAll"
              :title="matchLabel"
              :aria-label="matchLabel"
              onLabel="Match All"
              offLabel="Match Any"
              onIcon="pi pi-check-circle"
              offIcon="pi pi-circle"
            />
            <Button
              class="control clear-tags-btn"
              icon="pi pi-times"
              label=""
              title="Clear selected tags"
              aria-label="Clear selected tags"
              severity="secondary"
              outlined
              @click="clearSelectedTags"
            />
          </div>
        </div>
        <!-- Locations group -->
        <div class="filter-group filter-locations">
          <label class="group-label" for="locationsInput" id="locationsLabel">Locations</label>
          <MultiSelect
            id="locationsSelect"
            class="location-multiselect filter-select"
            v-model="selectedLocationIds"
            :options="displayedLocations"
            optionLabel="title"
            optionValue="id"
            display="chip"
            filter
            placeholder="Filter by location(s)"
            :maxSelectedLabels="4"
            inputId="locationsInput"
            aria-labelledby="locationsLabel"
            @filter="onTagFilter"
          />
        </div>
        <!-- Per-page group -->
        <div class="filter-group filter-perpage">
          <span class="group-label" id="perPageLabel">Art per page</span>
          <Dropdown
            id="perPageSelect"
            class="per-page-select"
            v-model="perPage"
            :options="paginationOptions"
            inputId="perPageInput"
            aria-labelledby="perPageLabel"
          />
        </div>
      </div>

      <!-- Banner (optional) -->
      <div v-if="bannerText" class="tag-banner">
        Showing {{ bannerText }} Art
      </div>
    </header>

    <!-- Art Grid / Feed -->
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
:deep(.p-button.p-button-outlined) {
  margin-left: 10px;
}

/* Container */
.gallery-container {
  width: 100%;
  max-width: 1200px;
  padding: 0 1rem;
  box-sizing: border-box;
  overflow-x: hidden;
}
.page-title {
  font-size: 1.4rem;
  font-weight: 800;
  margin: 0.7rem 0 0.2rem;
  padding-top: 10px;
}

/* Shared row base */
.row {
  display: grid;
  align-items: center;
  gap: .6rem;
  margin-bottom: .65rem;
}

/* ROW 1 Layout (smoothly condenses) */
.row-top {
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
}

.title-input,
.artist-input {
  min-width: 180px;
}

.date-dropdown {
  min-width: 130px;
  justify-self: start;
}

.newest-toggle {
  min-width: 150px;
  justify-self: start;
}

/* smooth intermediate breakpoint so search inputs don't collapse just under 1100px */
@media (min-width: 800px) and (max-width: 1099px) {
  .row-top {
    grid-template-columns:
      minmax(220px, 1fr)   /* title */
      minmax(220px, 1fr)   /* artist */
      minmax(140px, 180px) /* date */
      minmax(140px, 180px);/* newest toggle */
  }
}

/* Fixed 4-column alignment at wider screens */
@media (min-width: 1100px) {
  .row-top {
    grid-template-columns:
      minmax(260px, 1fr)
      minmax(260px, 1fr)
      minmax(140px, 160px)
      minmax(150px, 170px);
  }
}

/* ROW 2 Layout */
.row-filters {
  --filter-wide-min: 280px;      /* ← change to widen Tags / Locations min width */
  --filter-mid-min: 180px;       /* ← change for match/clear group min width */
  --filter-perpage-min: 160px;   /* ← change for per-page group min width */
  display: grid;
  gap: .75rem;
  align-items: start;
  margin-bottom: .65rem;
  grid-template-columns: repeat(auto-fit, minmax(var(--filter-mid-min), 1fr));
}

.filter-group {
  display: flex;
  flex-direction: column;
  gap: .35rem;
  min-width: var(--filter-mid-min);
}

.filter-tags,
.filter-locations {
  min-width: var(--filter-wide-min);
}

.filter-perpage {
  min-width: var(--filter-perpage-min);
}

.group-label {
  font-weight: 700;
  font-size: .85rem;
  line-height: 1;
  padding-left: .15rem;
  opacity: .95;
  user-select: none;
}

.filter-select :deep(.p-multiselect),
.filter-perpage :deep(.p-dropdown),
.per-page-select {
  width: 100%;
  box-sizing: border-box;
}

.filter-inline {
  display: flex;
  flex-wrap: wrap;
  gap: .5rem;
  align-items: center;
  min-width: var(--filter-mid-min);
  padding-top: 1.35rem; /* aligns with labels above other controls */
}

/* Wide screens: allocate explicit column widths for better balance */
@media (min-width: 1100px) {
  .row-filters {
    grid-template-columns:
      minmax(var(--filter-wide-min), 1fr)
      minmax(var(--filter-mid-min), 220px)
      minmax(var(--filter-wide-min), 1fr)
      minmax(var(--filter-perpage-min), 190px);
  }
}

/* Mid range keeps wide groups larger */
@media (min-width: 800px) and (max-width: 1099px) {
  .row-filters {
    /* override wide min values to shrink */
    --filter-wide-min: 200px;
    --filter-mid-min: 160px;
    /* keep same column structure, just narrower minima */
    grid-template-columns:
      minmax(var(--filter-wide-min), 1fr)
      minmax(var(--filter-mid-min), 200px)
      minmax(var(--filter-wide-min), 1fr)
      minmax(var(--filter-perpage-min), 170px);
  }
}

/* Mobile stacking */
@media (max-width: 825px) {
  .row-filters {
    grid-template-columns: 1fr;
  }
  .filter-inline {
    padding-top: .35rem;
  }
  .filter-group,
  .filter-tags,
  .filter-locations,
  .filter-perpage {
    min-width: 0;
  }
}

.tags-label,
.locations-label { display: none; }

/* Banner */
.tag-banner {
  margin: .4rem 0 .9rem;
  font-size: .9rem;
  font-weight: 600;
  opacity: .85;
  text-align: center;
}

/* Art grid (unchanged) */
.gallery-feed {
  --art-card-min: 260px;
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(var(--art-card-min), 1fr));
  gap: 1rem;
  width: 100%;
  max-width: 1200px;
  margin: 0 auto 2rem;
  box-sizing: border-box;
  overflow-x: hidden;
  padding-top: 10px;
}
.feed-item { min-width: 0; }

/* Desktop: fixed 4 columns for wide screens */
@media (min-width: 1100px) {
  .gallery-feed {
    grid-template-columns: repeat(4, minmax(0, 1fr));
  }
}

/* Tablet/Mid: 3 columns between mobile and desktop */
@media (min-width: 826px) and (max-width: 1099px) {
  .gallery-feed {
    grid-template-columns: repeat(3, minmax(0, 1fr));
  }
}

/* Small tablets/large phones: 2 columns */
@media (min-width: 600px) and (max-width: 825px) {
  .gallery-feed {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}

/* Mobile: single column for very small screens */
@media (max-width: 599px) {
  .gallery-feed {
    grid-template-columns: repeat(1, minmax(0, 1fr));
  }
}

.gallery-feed :deep(img),
.gallery-feed :deep(canvas),
.gallery-feed :deep(video) {
  max-width: 100%;
  height: auto;
  display: block;
}

/* No results */
.no-results {
  text-align: center;
  opacity: .8;
  margin: 2rem 0;
}
.no-results i { font-size: 2rem; margin-bottom: .5rem; }

/* (keep all existing desktop / tablet styles above unchanged) */

/* ===== MOBILE REWORK TO IMITATE LEGACY VERSION (≤825px) ===
   - Row 1: Title + Artist (2 columns)
   - Row 2: Date + Newest First (2 columns)
   - Filters split into stacked groups: Tags (+ Match/Clear inline), Locations, Art per page
   - Wider multiselects become full width
*/
@media (max-width: 825px) {
  /* Search + sort rows */
  .row-top {
    display: grid;
    grid-template-columns: 1fr 1fr;
    grid-auto-rows: auto;
    gap: .55rem;
  }
  /* Row 1 */
  .row-top .title-input { grid-column: 1; grid-row: 1; width: 100%; }
  .row-top .artist-input { grid-column: 2; grid-row: 1; width: 100%; }
  /* Row 2 */
  .row-top .date-dropdown { grid-column: 1; grid-row: 2; width: 100%; }
  .row-top .newest-toggle { grid-column: 2; grid-row: 2; width: 100%; }

  .row-top :deep(.p-dropdown),
  .row-top :deep(.p-inputtext),
  .row-top :deep(.p-togglebutton) { width: 100%; }

  /* Replace complex grid filters with stacked legacy style */
  .row-filters {
    display: flex;
    flex-direction: column;
    gap: .9rem;
  }

  .filter-group,
  .filter-tags,
  .filter-locations,
  .filter-perpage {
    width: 100%;
    min-width: 0;
  }

  /* Tags group first */
  .filter-tags { order: 1; }
  .filter-tags :deep(.p-multiselect) { width: 100%; }

  /* Match/Clear inline directly under tags */
  .filter-inline {
    order: 2;
    display: flex;
    flex-direction: row;
    gap: .6rem;
    padding-top: .15rem;
    width: 100%;
  }
  .filter-inline .match-toggle,
  .filter-inline .clear-tags-btn { flex: 1 1 0; min-width: 0; }

  /* Locations group */
  .filter-locations { order: 3; }
  .filter-locations :deep(.p-multiselect) { width: 100%; }

  /* Art per page group */
  .filter-perpage { order: 4; }
  .filter-perpage :deep(.p-dropdown) { width: 100%; }

  /* Labels tighten */
  .group-label {
    font-size: .8rem;
    margin-bottom: .2rem;
    padding-left: .05rem;
  }
}

/* Extra-small devices: allow buttons wrap */
@media (max-width: 360px) {
  .filter-inline { flex-wrap: wrap; }
  .filter-inline .match-toggle,
  .filter-inline .clear-tags-btn { flex: 1 1 48%; }
}

/* ===== Mobile refinement: keep desktop exactly as-is.
   Put Tags select + Match toggle + Clear button on the SAME ROW.
   Give Tags and Locations separate responsive behavior. */
@media (max-width: 825px) {
  /* Build a 3-col grid just for the filters row on mobile */
  .row.row-filters {
    display: grid !important;                   /* override earlier mobile flex */
    grid-template-columns: 1fr auto auto;       /* tags grows | match | clear */
    grid-auto-rows: auto;
    gap: .6rem;
    align-items: end;                            /* line up bottoms neatly */
  }

  /* TAGS group: turn wrapper into pass-through so label/select can be placed in the grid */
  .filter-tags { display: contents; }

  /* Label spans full width above the row */
  .filter-tags > .group-label {
    grid-column: 1 / -1;
    margin-bottom: .15rem;
  }

  /* Tag multiselect occupies the flexible first column */
  .tag-multiselect {
    grid-column: 1;
    min-width: 0;
    width: 100%;
  }
  /* Tag dropdown sizing (independent from Locations) */
  .tag-multiselect :deep(.p-multiselect) {
    width: 100%;
    min-width: 0;
  }

  /* Place Match Any/All and Clear in columns 2 and 3 */
  .filter-inline { display: contents; }         /* let its children be grid items */
  .match-toggle { grid-column: 2; }
  .clear-tags-btn { grid-column: 3; }

  /* Make the two buttons visually align with the multiselect */
  .match-toggle :deep(.p-togglebutton),
  .clear-tags-btn :deep(.p-button) {
    height: 2.5rem;
    padding: .5rem .9rem;
  }

  /* LOCATIONS group: stays stacked under Tags row and spans full width */
  .filter-locations { grid-column: 1 / -1; }
  .location-multiselect {
    width: 100%;
    min-width: 0;
  }
  /* Location dropdown can behave differently from tags if needed */
  .location-multiselect :deep(.p-multiselect) {
    width: 100%;
    min-width: 0;
    /* Example: limit panel height for locations only */
    /* -- you can tweak/remove next line independently of tags */
    /* max-width: none; */
  }

  /* ART PER PAGE: full width under locations */
  .filter-perpage { grid-column: 1 / -1; }
  .filter-perpage :deep(.p-dropdown) { width: 100%; }

  /* Tighten mobile labels a bit */
  .group-label { font-size: .8rem; }
}

/* Narrow safety: if the three items cannot fit, gracefully stack toggle/clear below */
@media (max-width: 370px) {
  .row.row-filters { grid-template-columns: 1fr; }
  .filter-inline { display: flex; gap: .5rem; }
  .match-toggle, .clear-tags-btn { flex: 1 1 0; }
}

/* Add override so on mobile the Locations and Art per page share one row */
@media (max-width: 825px) {
  /* Keep existing 3-column grid: 1fr auto auto.
     Put Locations in column 1; Art per page spans columns 2–3. */
  .row.row-filters {
    grid-template-columns: 1fr auto auto;
  }
  .filter-locations {
    grid-column: 1;
    margin-top: .25rem;
  }
  .filter-perpage {
    grid-column: 2 / 4;
    margin-top: .25rem;
    align-self: end;
  }
  .filter-perpage :deep(.p-dropdown) {
    width: 100%;
    min-width: 0;
  }
}

/* Ultra narrow: stack again (already present, keep behavior) */
@media (max-width: 370px) {
  .row.row-filters { grid-template-columns: 1fr; }
  .filter-locations,
  .filter-perpage { grid-column: 1 / -1; }
}
</style>

<!-- 1 -->