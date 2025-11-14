<template>
  <div class="landing p-8">
    <div class="text-4xl md:text-8xl font-bold">Pixel Painter</div>
    <div class="text-xl md:text-5xl mt-2">A somewhat minimalist painting application</div>

    <!-- small advisory shown only on small screens -->
    <div v-if="isSmallScreen" class="mobile-note">*For the best painting experience you must be on a PC</div>

    <Button
      class="mt-4 text-lg md:text-2xl"
      :label="buttonLabel"
      @click="onPrimaryClick"
    />

    <img
      src="../assets/images/pop_cat.gif"
      alt="cat"
      class="pop-cat" />
  </div>
</template>

<script setup lang="ts">
import Button from "primevue/button";
import router from "@/router";
import { ref, computed, onMounted, onBeforeUnmount } from "vue";

const isSmallScreen = ref(false);
let mq: MediaQueryList | null = null;

function updateMatch(e: MediaQueryListEvent | MediaQueryList) {
  // MediaQueryListEvent for change event, MediaQueryList for initial check
  isSmallScreen.value = (e as MediaQueryList).matches ?? false;
}

onMounted(() => {
  if (typeof window !== "undefined" && "matchMedia" in window) {
    mq = window.matchMedia("(max-width: 1000px)");
    // set initial
    isSmallScreen.value = mq.matches;
    // prefer addEventListener when available
    if ("addEventListener" in mq) {
      mq.addEventListener("change", updateMatch as EventListener);
    } else {
      // fallback for older browsers
      // @ts-ignore
      mq.addListener(updateMatch);
    }
  }
});

onBeforeUnmount(() => {
  if (mq) {
    if ("removeEventListener" in mq) {
      mq.removeEventListener("change", updateMatch as EventListener);
    } else {
      // @ts-ignore
      mq.removeListener(updateMatch);
    }
  }
});

const buttonLabel = computed(() => (isSmallScreen.value ? "See Gallery" : "Get Started"));

function onPrimaryClick() {
  if (isSmallScreen.value) {
    router.push("/gallery");
  } else {
    router.push("/new");
  }
}
</script>

<style scoped>
/* Keep the cat fixed on the bottom-right but constrain with viewport height,
   and reduce its size on small screens so it doesn't cover content. */
.pop-cat {
  position: fixed;
  bottom: 0;
  right: 0;
  max-height: 60vh; /* desktop behavior similar to previous 60% */
  width: auto;
  pointer-events: none;
  user-select: none;
}

/* Give content space so the image doesn't overlap important parts on small screens */
.landing {
  min-height: 100vh;
  box-sizing: border-box;
  padding-bottom: 20vh;
  padding: 2rem; /* default mobile */
}

/* Mobile-specific tweaks */
@media (max-width: 640px) {
  .pop-cat {
    max-height: 40vh;
    right: 0.5rem;
    bottom: 0.25rem;
  }
  .landing {
    padding: 1rem;
    padding-bottom: 12vh;
  }
}

/* Ensure desktop keeps the larger image */
@media (min-width: 1024px) {
  .pop-cat { max-height: 60vh; }
}

/* Padding for different screen sizes */
@media (min-width: 768px) {
  .landing { padding: 2rem 4rem; } /* tablet+ */
}
@media (min-width: 1024px) {
  .landing { padding: 3rem 6rem; } /* desktop */
}

/* small gray advisory for mobile users */
.mobile-note {
  color: #6b6b6b;
  font-size: 0.85rem;
  margin-top: 0.6rem;
  margin-bottom: 0.35rem;
  line-height: 1.2;
}

/* keep desktop unchanged; ensure note doesn't show on larger screens
   (note is controlled by v-if but keep style defensive) */
@media (min-width: 1001px) {
  .mobile-note { display: none !important; }
}
</style>

<!-- 3 -->