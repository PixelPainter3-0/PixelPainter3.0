<template>
  <div class="grid-view-root">
    <div v-if="loading" class="loading-overlay">
      <div class="loading-box">
        <div class="spinner"></div>
        <div>Loading grid…</div>
      </div>
    </div>
    <DrawingCanvas
      ref="canvas"
      :style="{ cursor: cursor.selectedTool.cursor }"
      :grid="gridCanvas"
      :showLayers="showLayers"
      :greyscale="greyscale"
      :isGrid="true"
      v-model="cursor"
      @mousedown="handleClickDraw()"
      @contextmenu.prevent
    />
  </div>
  <Toolbar class="fixed bottom-0 left-0 right-0 m-2">
    <template #start>
      <UploadButton
        v-if="artist.isAdmin"
        :art="art"
        :fps="fps"
        
        @OpenModal="toggleKeybinds"
      />
      <SaveImageToFile
        :art="art"
        :fps="fps"
        :grid="gridCanvas"
        :isGrid="true"
        :filtered="false"
        :filtered-art="''"
        :gif-from-viewer="['']"
      >
      </SaveImageToFile>
      <div class="ml-2">
        <div v-if="timeOuts.length < 5">
          Pixels Allowed: {{  (5-timeOuts.length) }}
        </div>
        <div v-else>
          Time till next Pixel: {{ countdown }}
        </div>
      </div>
    </template>
    <template #center>
      <ColorSelection
        v-if="cursor?.selectedTool?.cursor"
        v-model:color="cursor.color"
        v-model:size="cursor.size"
        :isBackground="false"
        :isGrid="true"
        @enable-key-binds="keyBindActive = true"
        @disable-key-binds="keyBindActive = false"
      />
      <BrushSelection v-model="cursor.selectedTool" :isGrid="true" />
      <Button
        icon="pi pi-expand"
        class="mr-2"
        severity="secondary"
        label=""
        title="Recenter"
        @click="canvas?.recenter()"
      />
      <HelpPopUp :isGrid="true" />
    </template>
    <template #end>
      <Button
        class="mr-2"
        :label="started ? 'Stop Music' : 'Start Music'"
        :icon="started ? 'pi pi-stop' : 'pi pi-play'"
        :severity="started ? 'danger' : 'success'"
        @click="toggleAudio"
      />
      <AudioSelect 
        v-model:volume="volume"
        @toggle-mute="toggleMute"
      />
    </template>
  </Toolbar>
</template>
<script setup lang="ts">
//vue prime
import Toolbar from "primevue/toolbar";
import Button from "primevue/button";

//custom components
import DrawingCanvas from "@/components/PainterUi/DrawingCanvas.vue";
import BrushSelection from "@/components/PainterUi/BrushSelection.vue";
import ColorSelection from "@/components/PainterUi/ColorSelection.vue";
import UploadButton from "@/components/PainterUi/UploadButton.vue";
import SaveImageToFile from "@/components/PainterUi/SaveImageToFile.vue";
import AudioSelect from "@/components/PainterUi/AudioSelect.vue";

//entities
import { PixelGrid } from "@/entities/PixelGrid";
import { Vector2 } from "@/entities/Vector2";
import PainterTool from "@/entities/PainterTool";
import Cursor from "@/entities/Cursor";
import { Pixel } from "@/entities/Pixel";
import Artist from "@/entities/Artist";

//services
import LoginService from "@/services/LoginService";

//vue
import { ref, watch, computed, onMounted, onUnmounted, nextTick, onBeforeUnmount } from "vue";
import router from "@/router";
import { useRoute } from "vue-router";
import { useToast } from "primevue/usetoast";

//scripts
import ArtAccessService from "@/services/ArtAccessService";
import Art from "@/entities/Art";

//Other
import * as SignalR from "@microsoft/signalr";
import { useArtistStore } from "@/store/ArtistStore";
import HelpPopUp from "@/components/PainterUi/HelpPopUp.vue";
import { useSignalStore } from "@/store/GridConnectStore";
import { bus } from '@/bus/GridBus';

//variables
const route = useRoute();
const canvas = ref<any>();
const toast = useToast();
const keyBindActive = ref<boolean>(true);
const artist = ref<Artist>(new Artist());
const resolution = ref<number>(200);
const backgroundColor = ref<string>("ffffff");
const isImage = ref<boolean>(true);
const gridCanvas = ref(
  new PixelGrid(
    resolution.value,
    resolution.value,
    backgroundColor.value.toUpperCase(),
    !isImage.value // Constructor wants isGif so pass in !isImage
  )
);
const artistStore = useArtistStore();
const gridConnection = useSignalStore();
const showLayers = ref<boolean>(true);
const greyscale = ref<boolean>(false);
const loggedIn = ref<boolean>(false);
const timeOuts = ref<Date[]>([]);
const countdown = ref<string>("0:00");
const timeLeft = ref<number>(0);
let countdownInterval: number | undefined = undefined;
const loading = ref<boolean>(true);
const audioFiles = [
  "/src/music/In-the-hall-of-the-mountain-king.mp3",
  "/src/music/flight-of-the-bumblebee.mp3",
  "/src/music/OrchestralSuiteNo3.mp3"
];
const audioRef = ref(new Audio());

const started = ref(false);
const volume = ref(50);
const audioIndex = ref(0);

const audio = ref<HTMLAudioElement | null>(null);

const toggleAudio = () => {
  if (!started.value) {
    // Start the audio
    started.value = true;
    audio.value = new Audio(audioFiles[audioIndex.value]);
    audio.value.loop = true;
    audio.value.volume = volume.value / 100;

    audio.value.play().catch((err) => {
      console.warn("Playback blocked:", err);
    });
  } else {
    // Stop the audio
    started.value = false;
    if (audio.value) {
      audio.value.pause();
      audio.value.currentTime = 0; // reset to beginning
      audio.value = null;
    }
  }
};
const previousVolume = ref(50);
const toggleMute = () => {
  if (!audio.value) return;

  if (volume.value > 0) {
    previousVolume.value = volume.value;
    volume.value = 0;
  } else {
    volume.value = previousVolume.value;
  }
};

watch(volume, (newVal) => {
  if (audio.value && started.value) {
    audio.value.volume = newVal / 100;
  }
});

const cursor = ref<Cursor>(
  new Cursor(new Vector2(-1, -1), PainterTool.getDefaults()[1], 1, "000000")
);
const startPix = ref<Vector2>(new Vector2(0, 0));
const endPix = ref<Vector2>(new Vector2(0, 0));
let tempGrid: string[][] = [];

const art = ref<Art>(new Art());

const fps = ref<number>(4);
const currentPallet = ref<string[]>([]);
function updatePallet() {
  let temp = localStorage.getItem("currentPallet");
  if (temp) currentPallet.value = JSON.parse(temp);
  for (let i = 0; i < currentPallet.value.length; i++)
    if (currentPallet.value[i] === null || currentPallet.value[i] === "") {
      currentPallet.value[i] = "000000";
    }
}
const cursorPositionComputed = computed(
  //default vue watchers can't watch deep properties
  //it can only watch individual references to the object specified
  //since when cursor position changes, the object holding the cursor position doesn't change
  //thus the watcher won't trigger
  //in order to get around this, we can use a computed property
  //a computed propert is updated every time a dependency changes
  //this computed property will return a new object every time the cursor position changes
  //thus the watcher watching this value will trigger with the old and new values
  //vue likes to be funky like that :3
  () => new Vector2(cursor.value.position.x, cursor.value.position.y)
);

onMounted(async () => {
  document.addEventListener("keydown", handleKeyDown);
  window.addEventListener("beforeunload", handleBeforeUnload);

  //Get the current user
  loading.value = true;
  loggedIn.value = await LoginService.isLoggedIn();

  if (loggedIn.value) {
    LoginService.getCurrentUser().then((user: Artist) => {
      artist.value = user;

      console.log("Artist Info:", artist.value);
      gridConnection.start(artist.value);
    });
  } else {
    artist.value.id = 0;
    artist.value.name = "Guest";
    cursor.value.selectedTool = PainterTool.getDefaults()[0];
  }
  //connect();
  bus.on('timeouts', (dates: unknown) => {
    timeOuts.value = ((dates as Date[]) || []).map(d => new Date(d));
    if (timeOuts.value.length >= 1) {
      setTimeLeft();
    }
  });

  bus.on('receivePixel', (data: unknown) => {
    const payload = data as { layer: number; color: string; coord: Vector2 };
    drawPixels(payload.layer, payload.color, [payload.coord]);
  });

  bus.on('gridConfig', async (data: unknown) => {
    const payload = data as {canvasSize: number, backgroundColor: string, pixels: Pixel[][]};
    art.value.pixelGrid.width = payload.canvasSize;
    art.value.pixelGrid.height = payload.canvasSize;
    art.value.pixelGrid.backgroundColor = payload.backgroundColor;
    art.value.pixelGrid.grid = art.value.pixelGrid.createGrid(
      payload.canvasSize,
      payload.canvasSize
    );
    await replaceCanvas(payload.pixels);
    loading.value = false;
  });
  
});

onUnmounted(() => {
  document.removeEventListener("keydown", handleKeyDown);
  window.removeEventListener("beforeunload", handleBeforeUnload);
  timeOuts.value = [];
  stop();
});

function handleBeforeUnload(event: BeforeUnloadEvent) {
  if (gridConnection.connection) {
    gridConnection.stop(artist.value);
    //connection.stop()
  }
  artistStore.save();
}

function toggleKeybinds(disable: boolean) {
  if (disable) {
    document.removeEventListener("keydown", handleKeyDown);
  } else {
    document.addEventListener("keydown", handleKeyDown);
  }
}

//functions
function formatMs(ms: number) {
  if (ms <= 0) {
    timeOuts.value.shift();
    return "0:00";
  }
  const minutes = Math.floor(ms / 60).toString().padStart(1, "0");
  const seconds = (ms % 60).toString().padStart(2, "00");
  return `${minutes}:${seconds}`;
}

const startCountdown = () => {
  if (countdownInterval) {
    return
  }
  countdown.value = formatMs(timeLeft.value);

  countdownInterval = window.setInterval(() => {
    if (timeLeft.value > 0) {
      timeLeft.value--;
      countdown.value = formatMs(timeLeft.value);
    } else {
      countdown.value = "0:00";
      stop()
    }
  }, 1000)
}

function setTimeLeft(){
  timeOuts.value = (timeOuts.value || []).map(d => new Date(d));
  const firstTs = timeOuts.value[0].getTime();
  const elapsedSec = Math.floor((Date.now() - firstTs) / 1000);
  const fiveMinutes = 5 * 60;
  timeLeft.value = Math.max(0, fiveMinutes - elapsedSec);
  startCountdown();
}

const stop = () => {
  clearInterval(countdownInterval);
  countdownInterval = undefined;
  if(timeOuts.value.length > 0){
    setTimeLeft();
  }
}

async function waitForCanvas(retries = 30, delay = 50) {
  for (let i = 0; i < retries; i++) {
    if (canvas.value) return true;
    // give Vue / child component time to mount
    await new Promise((r) => setTimeout(r, delay));
  }
  return !!canvas.value;
}

async function replaceCanvas(pixels: Pixel[][]) {
  gridCanvas.value = new PixelGrid(
    art.value.pixelGrid.width,
    art.value.pixelGrid.height,
    art.value.pixelGrid.backgroundColor,
    false
  );
  for(let a = 0; a < pixels.length; a++){
    for (let p = 0; p < pixels.length; p++) {
      gridCanvas.value.grid[pixels[a][p].x][pixels[a][p].y] =
        pixels[a][p].color;
    }
  }
  // keep tempGrid in sync so other logic sees the new state
  tempGrid = JSON.parse(JSON.stringify(gridCanvas.value.grid));
  
  await nextTick();
  await waitForCanvas();

  canvas.value?.drawLayers(0);
  canvas.value?.recenter();
}

function drawPixels(layer: number, color: string, coords: Vector2[]) {
  for (const coord of coords) {
    gridCanvas.value.grid[coord.x][coord.y] = color;
    canvas.value?.updateCell(layer, coord.x, coord.y, color);
    tempGrid[coord.x][coord.y] = color;
  }
}

function sendPixels(color: string, coords: Vector2[]) {
  if( gridConnection.connected) {
    gridConnection.sendPixels(color, coords[0], artist.value.id);
  }
}

// Single-click draw handler used by the template @mousedown
function handleClickDraw() {
  const coord = new Vector2(cursor.value.position.x, cursor.value.position.y);
  drawAtCoords([coord]);
}

function drawAtCoords(coords: Vector2[]) {
  // Only draw the first coordinate provided (single-pixel per click)
  if (!coords || coords.length === 0) return;
  const coord = coords[0];
  if (
    coord.x < 0 ||
    coord.x >= gridCanvas.value.width ||
    coord.y < 0 ||
    coord.y >= gridCanvas.value.height
  )
    return;
  sendPixels(cursor.value.color, [coord]);
}

//Save to file functions
function flattenArt(): string[][] {
  let width = gridCanvas.value.width;
  let height = gridCanvas.value.height;
  let arr: string[][] = Array.from({ length: height }, () =>
    Array(width).fill(gridCanvas.value.backgroundColor.toLowerCase())
  );
  return arr;
}

async function saveToFile(): Promise<void> {
  const grid: string[][] = flattenArt();

  const canvas = document.createElement("canvas");
  const context = canvas.getContext("2d");
  if (!context) {
    throw new Error("Could not get context");
  }
  const image = context.createImageData(grid.length, grid.length);

  canvas.width = grid.length;
  canvas.height = grid.length;

  for (let x = 0; x < grid.length; x++) {
    for (let y = 0; y < grid.length; y++) {
      let pixelHex = grid[x][y];
      pixelHex = pixelHex.replace("#", "").toUpperCase();
      const index = (x + y * grid.length) * 4;
      image?.data.set(
        [
          parseInt(pixelHex.substring(0, 2), 16),
          parseInt(pixelHex.substring(2, 4), 16),
          parseInt(pixelHex.substring(4, 6), 16),
          255
        ],
        index
      );
    }
  }
  context?.putImageData(image, 0, 0);

  //upscale the image to 1080
  var upsizedCanvas = document.createElement("canvas");
  upsizedCanvas.width = 1080;
  upsizedCanvas.height = 1080;
  var upsizedContext = upsizedCanvas.getContext("2d");
  if (!upsizedContext) {
    throw new Error("Could not get context");
  }
  upsizedContext.imageSmoothingEnabled = false; // Disable image smoothing
  upsizedContext.drawImage(canvas, 0, 0, 1080, 1080);

  const link = document.createElement("a");
  link.download = "image.png";
  link.href = upsizedCanvas.toDataURL("image/png");
  link.click();
}

function handleKeyDown(event: KeyboardEvent) {
  if (keyBindActive.value) {
    if (event.key === "p") {
      event.preventDefault();
      cursor.value.selectedTool.label = "Pan";
      canvas?.value.updateCursor();
    } else if (event.key === "b" && loggedIn.value) {
      event.preventDefault();
      cursor.value.selectedTool.label = "Brush";
      canvas?.value.updateCursor();
    } else if (event.key === "d" && loggedIn.value) {
      event.preventDefault();
      cursor.value.selectedTool.label = "Pipette";
      canvas?.value.updateCursor();
    } else if (event.key === "1") {
      event.preventDefault();
      updatePallet();
      //@ts-ignore
      cursor.value.color = currentPallet.value._value[0];
    } else if (event.key === "2") {
      event.preventDefault();
      updatePallet();
      //@ts-ignore
      cursor.value.color = currentPallet.value._value[1];
    } else if (event.key === "3") {
      event.preventDefault();
      updatePallet();
      //@ts-ignore
      cursor.value.color = currentPallet.value._value[2];
    } else if (event.key === "4") {
      event.preventDefault();
      updatePallet();
      //@ts-ignore
      cursor.value.color = currentPallet.value._value[3];
    } else if (event.key === "5") {
      event.preventDefault();
      updatePallet();
      //@ts-ignore
      cursor.value.color = currentPallet.value._value[4];
    } else if (event.key === "6") {
      event.preventDefault();
      updatePallet();
      //@ts-ignore
      cursor.value.color = currentPallet.value._value[5];
    } else if (event.key === "7") {
      event.preventDefault();
      updatePallet();
      //@ts-ignore
      cursor.value.color = currentPallet.value._value[6];
    } else if (event.key === "8") {
      event.preventDefault();
      updatePallet();
      //@ts-ignore
      cursor.value.color = currentPallet.value._value[7];
    } else if (event.key === "9") {
      event.preventDefault();
      updatePallet();
      //@ts-ignore
      cursor.value.color = currentPallet.value._value[8];
    } else if (event.key === "0") {
      event.preventDefault();
      updatePallet();
      //@ts-ignore
      cursor.value.color = currentPallet.value._value[9];
    } else if (event.key === "-") {
      event.preventDefault();
      updatePallet();
      //@ts-ignore
      cursor.value.color = currentPallet.value._value[10];
    } else if (event.key === "=") {
      event.preventDefault();
      updatePallet();
      //@ts-ignore
      cursor.value.color = currentPallet.value._value[11];
    } else if (event.ctrlKey && event.key === "d") {
      event.preventDefault();
      saveToFile();
    }
  }
}
</script>
<style scoped>
/* simple overlay spinner — adjust styling to your app theme */
.grid-view-root { position: relative; min-height: 200px; }
.loading-overlay {
  position: fixed;
  inset: 0;
  display:flex;
  align-items:center;
  justify-content:center;
  background: rgba(0,0,0,0.45);
  z-index: 9999;
}
.loading-box {
  display:flex;
  flex-direction:column;
  align-items:center;
  gap:10px;
  padding:14px 18px;
  border-radius:8px;
  background: rgba(255,255,255,0.95);
  color:#222;
}
.spinner {
  width:28px;
  height:28px;
  border-radius:50%;
  border:4px solid rgba(0,0,0,0.1);
  border-top-color: #333;
  animation: spin 0.9s linear infinite;
}
@keyframes spin { to { transform: rotate(360deg); } }
</style>