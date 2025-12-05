<template>
  <div class="pagination" v-if="pages > 1">
    <!--Only have pagination if there are more than one page-->
    <button v-if="pages != 2" :disabled="page === 1" @click="page = 1" aria-label="First page" title="First page">
      &lt;&lt;
    </button>
    <!-- Prev: PrimeVue style like AdminView -->
    <Button
      text
      :disabled="page === 1"
      label="Prev"
      icon="pi pi-chevron-left"
      aria-label="Previous page"
      title="Previous page"
      @click="page = Math.max(1, page - 1)"
    />
    <div v-if="pages < 16" class="buttonHolder">
      <!--Lists off all the pages-->
      <Button
        v-for="index in pages"
        :key="index"
        text
        :label="String(index)"
        :aria-current="index == page ? 'page' : undefined"
        :title="`Page ${index}`"
        :class="index == page ? 'page-selected' : ''"
        @click="page = index"
      />
    </div>
    <div v-else-if="startIndex == 1" class="buttonHolder">
      <!--Lists off all the pages-->
      <Button
        v-for="index in maxButtons + 1"
        :key="index"
        text
        :label="String(index)"
        :aria-current="index == page ? 'page' : undefined"
        :title="`Page ${index}`"
        :class="index == page ? 'page-selected' : ''"
        @click="page = index"
      />
      <span class="ellipsis" aria-hidden="true">…</span>
    </div>
    <div v-else-if="startIndex >= pages - 13" class="buttonHolder">
      <!--Lists off all the pages-->
      <span class="ellipsis" aria-hidden="true">…</span>
      <Button
        v-for="index in maxButtons + 1"
        :key="index"
        text
        :label="String(index + startIndex - 1)"
        :aria-current="index + startIndex - 1 == page ? 'page' : undefined"
        :title="`Page ${index + startIndex - 1}`"
        :class="index + startIndex - 1 == page ? 'page-selected' : ''"
        @click="page = index + startIndex - 1"
      />
    </div>
    <div v-else class="buttonHolder">
      <!--Lists off all the pages-->
      <span class="ellipsis" aria-hidden="true">…</span>
      <Button
        v-for="index in maxButtons"
        :key="index"
        text
        :label="String(index + startIndex - 1)"
        :aria-current="index + startIndex - 1 == page ? 'page' : undefined"
        :title="`Page ${index + startIndex - 1}`"
        :class="index + startIndex - 1 == page ? 'page-selected' : ''"
        @click="page = index + startIndex - 1"
      />
      <span class="ellipsis" aria-hidden="true">…</span>
    </div>
    <!-- Next: PrimeVue style like AdminView -->
    <Button
      text
      :disabled="page === pages"
      label="Next"
      icon="pi pi-chevron-right"
      iconPos="right"
      aria-label="Next page"
      title="Next page"
      @click="page = Math.min(pages, page + 1)"
    />
    <button
      v-if="pages != 2"
      :disabled="page == pages"
      @click="page = props.pages"
      aria-label="Last page" title="Last page">
      &gt;&gt;
    </button>
  </div>
</template>

<script setup lang="ts">
import { ref, defineProps, watch, defineEmits, computed } from "vue";
import Button from 'primevue/button';
const props = defineProps<{
  pages: number;
}>();
const emit = defineEmits(["pageChange"]);
const page = ref<number>(1);
const maxButtons = ref<number>(13);
const startIndex = computed(() => {
  if (props.pages > 15 && page.value > 7) {
    if (page.value + 7 >= props.pages) {
      return props.pages - 13;
    }
    return page.value - 6;
  }
  return 1;
});
watch(props, () => {
  page.value = 1;
});
watch(page, () => {
  emit("pageChange", page.value);
});
</script>
<style scoped>
.pagination {
  width: 100%;
  display: flex;
  justify-content: center;
  align-items: center;
  padding: 1rem;
  gap: 0.5rem; /* Space between groups */
  flex-wrap: wrap;

  /* Theming hooks with sensible fallbacks */
  --pg-fg: var(--text-color, #e5e7eb);
  --pg-bg: var(--surface-200, #1f2937);
  --pg-bg-hover: var(--surface-300, #273244);
  --pg-border: var(--surface-400, #374151);
  /* use PrimeVue theme tokens coming from DarkModeToggle/updatePreset */
  --pg-focus: var(--primary-400, var(--primary-300, #a0a0a0));
  --pg-selected-bg: var(--primary-color, #323233);
  --pg-selected-fg: #ffffff;
  --pg-selected-border: var(--primary-600, #858585);
  --pg-disabled-bg: var(--surface-100, #111827);
  --pg-disabled-fg: #9ca3af;
}
.buttonHolder {
  display: flex;
  width: auto;
  gap: 0.5rem;
  flex-wrap: wrap;
}
/* Ellipsis */
.ellipsis {
  color: var(--pg-fg);
  opacity: .8;
  padding: 0 .25rem;
}

/* Selected state for PrimeVue Button - match pink/blue theme like Prev/Next */
:deep(.p-button.page-selected) {
  color: var(--primary-500) !important; /* pink/primary text */
  background: transparent !important; /* keep text style */
  border: 1px solid var(--primary-500) !important; /* subtle highlight */
}
:deep(.p-button.page-selected:hover) {
  color: var(--primary-400) !important;
  border-color: var(--primary-400) !important;
}

/* Compact on very small screens */
@media (max-width: 420px) {
  .pagination { gap: .4rem; }
  .buttonHolder { gap: .4rem; }
  :deep(.p-button) { height: 2rem; }
}
</style>

