<template>
  <Button
    rounded
    :icon="checked ? 'pi pi-sun' : 'pi pi-moon'"
    @click="checked = !checked" />
</template>

<script setup lang="ts">
import Button from "primevue/button";
import { onMounted, ref, watch } from "vue";
import { useThemeStore } from "@/store/ThemeStore";
import { updatePreset } from "@primevue/themes";

const store = useThemeStore();
const checked = ref<boolean>(store.Theme === "light"); // true => light

const DARK_PRIMARY = {
  50:  "#fdf2f8",
  100: "#fce7f3",
  200: "#fbcfe8",
  300: "#f9a8d4",
  400: "#f472b6",
  500: "#ec4899", //hover color
  600: "#db2777", //primary color
  700: "#be185d",
  800: "#9d174d",
  900: "#831843",
  950: "#500724",
};
const LIGHT_PRIMARY_DARKER = {
  50:  "#f0f7ff",
  100: "#d9ecff",
  200: "#b9dcff",
  300: "#91c5ff",
  400: "#6baeff",
  500: "#2C73D9", // hover color (kept)
  600: "#1da1f2", // primary color (kept)
  700: "#1b5ecb",
  800: "#184ea9",
  900: "#153f88",
  950: "#113168",
};

function applyTheme(theme: "light" | "dark") {
  const html = document.documentElement;
  if (theme === "dark") {
    html.classList.add("dark-mode-toggle");
    updatePreset({ semantic: { primary: DARK_PRIMARY } });
  } else {
    html.classList.remove("dark-mode-toggle");
    updatePreset({ semantic: { primary: LIGHT_PRIMARY_DARKER } });
  }
}

watch(checked, (val) => {
  store.Theme = val ? "light" : "dark";
  applyTheme(val ? "light" : "dark");
});

onMounted(() => {
  // ensure UI and colors match persisted store theme
  checked.value = store.Theme === "light";
  applyTheme(store.Theme === "light" ? "light" : "dark");
});
</script>

