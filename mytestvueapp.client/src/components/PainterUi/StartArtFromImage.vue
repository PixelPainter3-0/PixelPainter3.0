<template>
  <Button label="Start From This" icon="pi pi-pencil" @click="handleClick()" v-if="!art.isGif"></Button>
</template>
<script setup lang="ts">
import Art from "@/entities/Art";
import Button from "primevue/button";
import { useLayerStore } from "@/store/LayerStore";

import router from "@/router";

const layerStore = useLayerStore();
const props = defineProps<{
  art: Art;
  fps: number;
  gifFromViewer?: string[];
  filtered?: boolean;
  filteredArt?: string;
}>();

function handleClick(): void {
	startFromArt();
}

async function startFromArt(): Promise<void> { 
  layerStore.pushGrid(props.art.pixelGrid);
  router.push("/paint");
}

</script>