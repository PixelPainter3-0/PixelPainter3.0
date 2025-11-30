<template>
  <!-- add dynamic class for hide-on-scroll -->
  <header :class="['header-shell', { 'hide-on-scroll': hideHeader }]" ref="rootEl">
    <Toolbar class="border-none custom-background">
      <template #start>
        <Button
          :class="['menu-toggle', 'p-button-text', { open: menuOpen }]"
          :icon="menuOpen ? 'pi pi-times' : 'pi pi-bars'"
          :aria-expanded="menuOpen ? 'true' : 'false'"
          aria-controls="mobile-menu"
          :aria-label="menuOpen ? 'Close menu' : 'Open menu'"
          @click="toggleMenu"
        />
        <RouterLink class="router-link-unstyled" to="/">
          <h1 class="m-0 ml-2 font-bold">
            <span style="color: var(--p-primary-color)">Pixel</span>Painter
          </h1>
        </RouterLink>
      </template>

      <template #center>
        <div class="nav-buttons">
          <RouterLink class="p-2" to="/">
            <Button rounded label="Home" icon="pi pi-home" />
          </RouterLink>
          <RouterLink class="p-2" to="/new">
            <Button rounded label="Painter" icon="pi pi-pencil" />
          </RouterLink>
          <RouterLink class="p-2" to="/thegrid">
            <Button rounded label="The Grid" icon="pi pi-globe" />
          </RouterLink>
          <RouterLink class="p-2" to="/map">
            <Button rounded label="Map" icon="pi pi-map" />
          </RouterLink>
          <RouterLink class="p-2" to="/gallery">
            <Button rounded label="Gallery" icon="pi pi-image" />
          </RouterLink>
        </div>

        <!-- Mobile stacked menu with smooth slide animation -->
        <Transition name="slide-down">
          <div v-if="menuOpen" id="mobile-menu" class="mobile-menu">
            <RouterLink to="/" class="mobile-link">
              <Button label="Home" icon="pi pi-home" @click="menuOpen = false" />
            </RouterLink>
            <RouterLink to="/gallery" class="mobile-link">
              <Button label="Gallery" icon="pi pi-image" @click="menuOpen = false" />
            </RouterLink>
            <RouterLink to="/map" class="mobile-link">
              <Button label="Map" icon="pi pi-map" @click="menuOpen = false" />
            </RouterLink>
            <RouterLink to="/notifications" v-if="isLoggedIn" class="mobile-link">
              <Button label="Notifications" icon="pi pi-bell" @click="menuOpen = false" />
            </RouterLink>
          </div>
        </Transition>
      </template>

      <template #end>
        <RouterLink class="mr-2 hide-on-mobile" to="/notifications" v-if="isLoggedIn">
          <Notification />
        </RouterLink>
        <DarkModeSwitcher class="mr-2" />
        <GoogleLogin />
      </template>
    </Toolbar>
  </header>
</template>

<script setup lang="ts">
import { RouterLink } from "vue-router";
import { ref, onMounted, onBeforeUnmount, watch } from "vue";
import router from "@/router";
import Button from "primevue/button";
import DarkModeSwitcher from "./DarkModeToggle.vue";
import GoogleLogin from "../GoogleLogin.vue";
import Toolbar from "primevue/toolbar";
import Notification from "./NotificationRedirect.vue";
import LoginService from "@/services/LoginService";
import { useLayerStore } from "@/store/LayerStore";
import { useArtistStore } from "@/store/ArtistStore";
import { PixelGrid } from "@/entities/PixelGrid";

const emit = defineEmits(["openModal", "connect", "disconnect"]);

const layerStore = useLayerStore();
const artistStore = useArtistStore();

const isLoggedIn = ref<boolean>(false);

const resolution = ref<number>(200);
const backgroundColor = ref<string>("ffffff");
const isImage = ref<boolean>(true);

// mobile menu open state
const menuOpen = ref(false);
const rootEl = ref<HTMLElement | null>(null);

// NEW: mobile hide-on-scroll state/logic
const hideHeader = ref(false);
let lastY = 0;
let ticking = false;
const DELTA = 8; // ignore tiny scroll jitter

function handleScroll() {
  const currentY = Math.max(0, window.scrollY || 0);
  const diff = currentY - lastY;

  // always show at top
  if (currentY <= 0) {
    hideHeader.value = false;
    lastY = currentY;
    ticking = false;
    return;
  }

  if (Math.abs(diff) > DELTA) {
    if (diff > 0 && currentY > 56 && !menuOpen.value) {
      // scrolling down
      hideHeader.value = true;
    } else if (diff < 0) {
      // scrolling up
      hideHeader.value = false;
    }
    lastY = currentY;
  }
  ticking = false;
}

function onScroll() {
  if (!ticking) {
    ticking = true;
    requestAnimationFrame(handleScroll);
  }
}

function toggleMenu() {
  menuOpen.value = !menuOpen.value;
  if (menuOpen.value) hideHeader.value = false; // keep header visible when menu is open
}

function updateLocalStorage(): void {
  layerStore.empty();
  let pixelGrid = new PixelGrid(
    resolution.value,
    resolution.value,
    backgroundColor.value.toUpperCase(),
    !isImage.value
  );
  router.push("/thegrid");
}

function onDocumentClick(e: MouseEvent) {
  if (!menuOpen.value) return;
  const target = e.target as Node;
  if (rootEl.value && !rootEl.value.contains(target)) {
    menuOpen.value = false;
  }
}

function onKeyUp(e: KeyboardEvent) {
  if (e.key === "Escape") menuOpen.value = false;
}

onMounted(async () => {
  layerStore.init();
  artistStore.init();

  LoginService.isLoggedIn().then((result) => {
    isLoggedIn.value = result;
  });

  document.addEventListener("click", onDocumentClick);
  document.addEventListener("keyup", onKeyUp);
  window.addEventListener("scroll", onScroll, { passive: true }); // NEW
});

onBeforeUnmount(() => {
  document.removeEventListener("click", onDocumentClick);
  document.removeEventListener("keyup", onKeyUp);
  window.removeEventListener("scroll", onScroll); // NEW
});

// Close the menu on route change and show header again
watch(
  () => router.currentRoute.value.fullPath,
  () => {
    menuOpen.value = false;
    hideHeader.value = false;
  }
);
</script>

<style scoped>
/* Theme variables (unscoped via :global so they actually apply) */
:global(:root) {
  /* Adjust transparency here: change the last value (alpha) from 0.62 to a lower number for more see-through,
     or a higher number for more solid. Example: 0.5 is more transparent, 0.9 is more solid. */
  --header-bg: rgba(255,255,255,0.6);
  --header-border: rgba(0,0,0,0.08);
  --header-shadow: 0 4px 18px -6px rgba(0,0,0,0.25);
}
:global(html.dark-mode-toggle) {
  /* Adjust transparency here for dark mode header (alpha = 0.54 below) */
  --header-bg: rgba(22,22,22,0.6);
  --header-border: rgba(255,255,255,0.14);
  --header-shadow: 0 6px 24px -8px rgba(0,0,0,0.6);
}

/* Shell + toolbar
   NOTE: header-shell is now sticky and animates via transform at ALL viewports
   so the same hide-on-scroll behavior will apply on desktop and mobile.
   If you want to tweak behavior specifically for desktop change the values below. */
.header-shell {
  position: sticky;
  top: 0;
  z-index: 1000;
  background: var(--header-bg);
  transform: translateY(0);
  transition: transform 220ms ease;
  will-change: transform;
}

/* applied when JS sets hideHeader = true */
.header-shell.hide-on-scroll {
  transform: translateY(-100%);
}

.custom-background {
  background: var(--header-bg);
  backdrop-filter: blur(12px);
  -webkit-backdrop-filter: blur(12px);
  border-bottom: 1px solid var(--header-border);
  box-shadow: var(--header-shadow);
  width: 100%;
}

/* Navigation (desktop) */
.nav-buttons {
  display: flex;
  gap: 0.5rem;
  align-items: center;
  flex-wrap: nowrap;
}

/* Hamburger (hidden desktop) */
.menu-toggle {
  display: none;
  margin-right: 0.25rem;
  transition: transform 200ms ease;
}
.menu-toggle.open .pi {
  transform: rotate(180deg) scale(1.05);
}

/* Mobile menu (hidden by default) */
.mobile-menu {
  display: none;
  width: 20%;
  min-width: 170px;
}

/* Mobile-specific rules */
@media (max-width: 1025px) {
  .menu-toggle { display: inline-flex; }
  .nav-buttons { display: none; }

  .mobile-menu {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
    padding: 0.65rem 0.75rem 0.9rem;
    background: var(--header-bg);
    backdrop-filter: blur(10px);
    -webkit-backdrop-filter: blur(10px);
    border-radius: 14px;
    position: absolute;
    top: 56px;
    left: 0.5rem;
    right: 0.5rem;
    z-index: 1050;
    box-shadow: var(--header-shadow);
    border: 1px solid var(--header-border);
  }
  .mobile-link .p-button {
    width: 100%;
    justify-content: flex-start;
    border-radius: 10px;
  }

  .custom-background h1 {
    font-size: 1.4rem;
    margin-left: 0.3rem;
  }

  .p-toolbar {
    padding-left: 0.55rem;
    padding-right: 0.55rem;
    min-height: 56px;
  }

  /* Make header controls (dark mode / google login / notification) smaller on mobile to match mobile breakpoint */
  @media (max-width: 1000px) {
    /* Target PrimeVue buttons inside the header toolbar (affects DarkModeSwitcher, GoogleLogin output, Notification button) */
    .custom-background .p-toolbar .p-button {
      padding: 0.32rem 0.5rem;
      font-size: 0.82rem;
      height: 36px;
      min-width: 36px;
    }

    /* Reduce icon sizes inside those buttons */
    .custom-background .p-toolbar .p-button .pi {
      font-size: 0.95rem;
    }

    /* DarkModeSwitcher has an extra class 'mr-2' in template — shrink that wrapper too */
    .custom-background .mr-2 {
      margin-right: 0.5rem;
      transform: scale(0.96);
      transform-origin: center;
    }
  }

  /* Extra compact styling for very small phones */
  @media (max-width: 420px) {
    .custom-background .p-toolbar .p-button {
      padding: 0.22rem 0.4rem;
      font-size: 0.75rem;
      height: 32px;
      min-width: 32px;
    }
    .custom-background .p-toolbar .p-button .pi {
      font-size: 0.85rem;
    }
    .custom-background .mr-2 {
      transform: scale(0.92);
    }
  }
    /* hide the header toolbar notification button on small screens */
  .hide-on-mobile {
    display: none !important;
  }
}

/* Transition (Vue <Transition name="slide-down">) */
.slide-down-enter-active,
.slide-down-leave-active {
  transition: max-height 220ms cubic-bezier(.2,.9,.3,1),
              opacity 180ms ease,
              transform 220ms ease;
  overflow: hidden;
}
.slide-down-enter-from,
.slide-down-leave-to {
  max-height: 0;
  opacity: 0;
  transform: translateY(-8px);
}
.slide-down-enter-to,
.slide-down-leave-from {
  max-height: 480px;
  opacity: 1;
  transform: translateY(0);
}
</style>

<!-- 6 -->