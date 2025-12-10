<template>
  <div>
    <canvas :id="canvasId" class="vertical-align-middle" ref="canvas"></canvas>
  </div>
</template>

<script setup lang="ts">
import type Art from "@/entities/Art";
import { ref, onMounted, watch, computed } from "vue";

const props = defineProps<{
  modelValue?: string;
  art: Art;
  pixelSize: number;
  canvasNumber?: number;
}>(); // v-model
const emit = defineEmits(["update:modelValue"]); // Event to update parent

const canvas = ref<HTMLCanvasElement | null>(null);
const ctx = ref<CanvasRenderingContext2D | null>(null);

// Computed property for syncing v-model
const localGrid = computed({
  get: () => props.modelValue, // Get the color from the parent
  set: (newGrid) => emit("update:modelValue", newGrid) // Send update to parent
});
const canvasId = computed(() => {
  return `viewer-page-canavs-${props.canvasNumber}`;
});
watch(props, () => {
  updateCanvas();
});

onMounted(() => {
  if (canvas.value) {
    ctx.value = canvas.value.getContext("2d");
    updateCanvas();
  }
});

// Watch for color changes (from parent or local input)
watch(localGrid, () => {
  drawSquare();
});

function drawSquare(): void {
  if (!ctx.value || !canvas.value) return;
  ctx.value.clearRect(0, 0, canvas.value.width, canvas.value.height); // Clear
  renderfilter();
}

function updateCanvas(): void {
  let canvasInit = document.getElementById(canvasId.value) as HTMLCanvasElement;
  if (canvasInit) {
    canvas.value = canvasInit;
    let contextInit = canvasInit.getContext("2d");
    if (contextInit) {
      ctx.value = contextInit;
      ctx.value.clearRect(0, 0, canvas.value.width, canvas.value.height);

      //There is almost certainly also an error when it's iterating through it in one of the render functions or elsewhere that
      //both causes a display error for horizontal canvases and a machine error for vertical canvases

      canvas.value.width = props.art.pixelGrid.width * props.pixelSize;
      canvas.value.height = props.art.pixelGrid.height * props.pixelSize;

        let wScale = 32;
        let hScale = 32;
        if (canvas.value.width >= canvas.value.height) {
            hScale = 32 * canvas.value.height / canvas.value.width;
        }
        else {
            wScale = 32 * canvas.value.width / canvas.value.height;
        }

      ctx.value.scale(
        wScale / props.art.pixelGrid.width,
        hScale / props.art.pixelGrid.height
      );
    }
  }
  render();
}
function render(): void {
  if (ctx.value && props.art.pixelGrid.encodedGrid) {
    const imageServe = props.art.pixelGrid.encodedGrid;
    let hexBegin = 0;
    let hexEnd = 6;
    for (
      let column = 0;
      column < props.art.pixelGrid.width * props.pixelSize;
      column += props.pixelSize
    ) {
      for (
        let row = 0;
        row < props.art.pixelGrid.height * props.pixelSize;
        row += props.pixelSize
      ) {
        ctx.value.fillStyle = "#" + imageServe.substring(hexBegin, hexEnd);
        ctx.value.fillRect(column, row, props.pixelSize, props.pixelSize);
        ctx.value.globalCompositeOperation = "lighter";
        hexBegin += 6;
        hexEnd += 6;
      }
    }
  }
}
function renderfilter(): void {
  if (ctx.value && props.art.pixelGrid.encodedGrid) {
    const imageServe = props.modelValue ? props.modelValue : "";
    let hexBegin = 0;
    let hexEnd = 6;
    for (
      let column = 0;
      column < props.art.pixelGrid.width * props.pixelSize;
      column += props.pixelSize
    ) {
      for (
        let row = 0;
        row < props.art.pixelGrid.height * props.pixelSize;
        row += props.pixelSize
      ) {
        ctx.value.fillStyle = "#" + imageServe.substring(hexBegin, hexEnd);
        ctx.value.fillRect(column, row, props.pixelSize, props.pixelSize);
        ctx.value.globalCompositeOperation = "lighter";
        hexBegin += 6;
        hexEnd += 6;
      }
    }
  }
}
</script>
