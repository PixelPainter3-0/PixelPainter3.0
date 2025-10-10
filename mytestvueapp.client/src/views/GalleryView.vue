<template>
  <div class="w-9 mx-auto my-0">
    <header>
      <h1 class="flex align-items-center gap-3">
        <span>Search for Art</span>
        <InputText
          class="mt-2"
          v-model.trim="search"
          type="text"
          placeholder="Search title..."
        />
        <InputText
          class="mt-2 w-2"
          v-model.trim="filter"
          type="text"
          placeholder="Search artists..."
        />
        <Dropdown
          class="pl mt-2 text-base w-1.5 font-normal"
          v-model="sortType"
          :options="sortBy"
          optionLabel="sort"
          optionValue="code"
          placeholder="Sort by"
        />
        <ToggleButton
          v-if="isSortedByDate"
          id="toggle"
          class="mt-2 text-base w-0 font-normal"
          v-model="checkAscending"
          onLabel="Oldest First"
          onIcon="pi pi-arrow-up"
          offLabel="Newest First"
          offIcon="pi pi-arrow-down"
          @click="sortGallery()"
        />
        <ToggleButton
          v-else
          id="toggle"
          class="mt-2 text-base w-0 font-normal"
          v-model="checkAscending"
          onLabel="Ascending"
          onIcon="pi pi-arrow-up"
          offLabel="Descending"
          offIcon="pi pi-arrow-down"
          @click="sortGallery()"
        />
      </h1>

      <!-- Centered tag title below the search bars -->
      <div v-if="activeTag" class="tag-banner">
        Showing '{{ activeTag }}' Art
      </div>

      <div style="display: inline-flex">
        <p>Art per page: &nbsp;</p>
        <Dropdown
          class="pl my-2 text-base w-1.5 font-normal"
          v-model="perPage"
          :options="paginationOptions"
        />
      </div>
    </header>
    <div class="shrink-limit flex flex-wrap" v-if="!loading">
      <ArtCard
        v-for="index in displayAmount"
        :key="index"
        :art="displayArt[index + offset]"
        :size="6"
        :position="index"
      />
    </div>
    <ArtPaginator :pages="pages" @page-change="changePage" />
  </div>
</template>

<script setup lang="ts">
import { onMounted, ref, watch, computed } from "vue";
import { useRoute } from "vue-router";
import ArtCard from "@/components/Gallery/ArtCard.vue";
import Art from "@/entities/Art";
import ArtAccessService from "@/services/ArtAccessService";
import InputText from "primevue/inputtext";
import Dropdown from "primevue/dropdown";
import ToggleButton from "primevue/togglebutton";
import ArtPaginator from "@/components/Gallery/ArtPaginator.vue";

const publicArt = ref<Art[]>([]);
const displayArt = ref<Art[]>([]);
const search = ref<string>("");
const filter = ref<string>("");
const loading = ref<boolean>(true);

// NEW: Active tag from route
const route = useRoute();
const activeTag = ref<string | null>((route.params.tag as string) || null);

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
const sortType = ref<string>("D"); // Value binded to sort drop down
const isSortedByDate = ref<boolean>(true);
const checkAscending = ref<boolean>(false);
const isModified = ref<boolean>(false);
const currentPage = ref<number>(1);
const perPage = ref<number>(12);
const flicker = ref<boolean>(true);
const pages = computed(() => {
  return Math.ceil(displayArt.value.length / perPage.value);
});
const displayAmount = computed(() => {
  if (currentPage.value == pages.value) {
    if (displayArt.value.length % perPage.value == 0) return perPage.value;
    return displayArt.value.length % perPage.value;
  }
  return perPage.value;
});
const offset = computed(() => {
  return perPage.value * (currentPage.value - 1) - 1;
});
watch(perPage, () => {
  flicker.value = false;
  changePage(1);
  flicker.value = true;
});

// Helper: recompute displayArt based on activeTag, search, and filter
function updateDisplay(): void {
  let list = publicArt.value;

  if (activeTag.value) {
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

  displayArt.value = list.slice(); // new array in current sorted order
  isModified.value = true;
  // reset to first page if current page would be out of range
  if (currentPage.value > Math.max(1, Math.ceil(displayArt.value.length / perPage.value))) {
    currentPage.value = 1;
  }
}

onMounted(async () => {
  ArtAccessService.getAllArt()
    .then((data) => {
      publicArt.value = data;
      // initial sort by date desc as before
      sortGallery();
      updateDisplay();
    })
    .finally(() => {
      loading.value = false;
    });
});

// Recompute when route tag changes
watch(
  () => route.params.tag,
  (newTag) => {
    activeTag.value = newTag ? String(newTag) : null;
    changePage(1);
    updateDisplay();
  }
);

// Search and artist filter now just recompute
watch(search, () => {
  updateDisplay();
});

watch(filter, () => {
  updateDisplay();
});

watch(sortType, () => {
  sortGallery();
});

function changePage(page: number): void {
  currentPage.value = page;
}

function sortGallery(): void {
  var sortCode = sortType.value;
  isModified.value = true;

  isSortedByDate.value = false;
  if (sortCode == "L") {
    if (checkAscending.value) {
      publicArt.value.sort((artA, artB) => artA.numLikes - artB.numLikes);
    } else {
      publicArt.value.sort((artA, artB) => artB.numLikes - artA.numLikes);
    }
  } else if (sortCode == "C") {
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
  // After sorting the master list, refresh the filtered view
  updateDisplay();
}
</script>

<style scoped>
.tag-banner {
  width: 100%;
  text-align: center;
  margin-top: 0.25rem;
  margin-bottom: 0.5rem;
  font-size: 1.15rem;
  font-weight: 700;
}
</style>
