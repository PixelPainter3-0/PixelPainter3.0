<template>
  <div class="pagination" v-if="pages > 1">
    <!--Only have pagination if there are more than one page-->
    <Button
      v-if="pages != 2"
      text
      :disabled="page === 1"
      label=""
      icon="pi pi-angle-double-left"
      aria-label="First page"
      title="First page"
      @click="page = 1"
    />
    <!-- Prev: PrimeVue style like AdminView -->
    <Button
      text
      :disabled="page === 1"
      class="pn-button prev"
      label="Prev"
      icon="pi pi-chevron-left"
      aria-label="Previous page"
      title="Previous page"
      @click="page = Math.max(1, page - 1)"
    />
    <div class="buttonHolder">
      <template v-for="(it, idx) in pageItems" :key="'pi-' + idx">
        <Button
          v-if="typeof it === 'number'"
          text
          :label="String(it)"
          :aria-current="it == page ? 'page' : undefined"
          :title="`Page ${it}`"
          :class="it == page ? 'page-selected' : ''"
          @click="page = it"
        />
        <span v-else class="ellipsis" aria-hidden="true">…</span>
      </template>
    </div>
    <!-- Next: PrimeVue style like AdminView -->
    <Button
      text
      :disabled="page === pages"
      class="pn-button next"
      label="Next"
      icon="pi pi-chevron-right"
      iconPos="right"
      aria-label="Next page"
      title="Next page"
      @click="page = Math.min(pages, page + 1)"
    />
    <!-- Last: PrimeVue style to match rest of paginator -->
    <Button
      v-if="pages != 2"
      text
      :disabled="page === pages"
      label=""
      icon="pi pi-angle-double-right"
      iconPos="right"
      aria-label="Last page"
      title="Last page"
      @click="page = props.pages"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, defineProps, watch, defineEmits, computed, onMounted, onBeforeUnmount } from "vue";
import Button from 'primevue/button';
const props = defineProps<{
  pages: number;
}>();
const emit = defineEmits(["pageChange"]);
const page = ref<number>(1);
const maxButtons = ref<number>(13);

function calcMaxButtons() {
  const w = window.innerWidth || 1920;
  let m = 13;
  if (w < 480) m = 2;
  else if (w < 600) m = 2;
  else if (w < 740) m = 4;
  else if (w < 900) m = 6;
  else if (w < 1120) m = 8;
  else m = 13;
  maxButtons.value = m;
}

const pageItems = computed<(number | 'ellipsis')[]>(() => {
  const P = props.pages || 0;
  const p = Math.min(Math.max(1, page.value), P);
  const M = Math.max(3, maxButtons.value);

  if (P <= M) return Array.from({ length: P }, (_, i) => i + 1);

  const items: (number | 'ellipsis')[] = [];
  items.push(1);
  const innerCount = M - 2; // reserve first and last
  let left = Math.max(2, p - Math.floor(innerCount / 2));
  let right = Math.min(P - 1, left + innerCount - 1);

  // adjust left if we are near the end
  if (right - left + 1 < innerCount) {
    left = Math.max(2, right - innerCount + 1);
  }

  if (left > 2) items.push('ellipsis');
  for (let i = left; i <= right; i++) items.push(i);
  if (right < P - 1) items.push('ellipsis');
  items.push(P);
  return items;
});

function onResize() { calcMaxButtons(); }
onMounted(() => {
  calcMaxButtons();
  window.addEventListener('resize', onResize, { passive: true });
});
onBeforeUnmount(() => {
  window.removeEventListener('resize', onResize);
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

/* Hide Prev/Next text and start shrinking at <= 510px */
@media (max-width: 510px) {
  .pagination { gap: .35rem; }
  .buttonHolder { gap: .35rem; }

  /* Hide labels for prev/next only, keep numbers visible */
  :deep(.pn-button .p-button-label) { display: none !important; }

  /* tighten general button sizing */
  :deep(.p-button) {
    height: 1.9rem;
    min-width: 1.9rem;
    padding: 0 .4rem;
  }
  /* slightly smaller chevron icons for prev/next */
  :deep(.pn-button .pi) { font-size: 0.9rem; }
}
</style>

