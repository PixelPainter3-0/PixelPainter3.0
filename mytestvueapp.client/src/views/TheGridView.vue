<template>
  <DrawingCanvas
    ref="canvas"
    :style="{ cursor: cursor.selectedTool.cursor }"
    :grid="gridCanvas"
    :showLayers="showLayers"
    :greyscale="greyscale"
    :isGrid="true"
    v-model="cursor"
    @mousedown="
      mouseButtonHeldDown = true;
      setStartVector();
      setEndVector();
    "
    @mouseup="
      mouseButtonHeldDown = false;
      setEndVector();
      onMouseUp();
    "
    @contextmenu.prevent
  />
  <Toolbar class="fixed bottom-0 left-0 right-0 m-2" v-if="loggedIn">
    <template #start>
      <UploadButton
        v-if="artist.isAdmin"
        :art="art"
        :fps="fps"
        :connection="connection"
        :connected="connected"
        :group-name="groupName"
        @disconnect="disconnect"
        @OpenModal="toggleKeybinds"
      />
      <SaveImageToFile
        :art="art"
        :fps="fps"
        :filtered="false"
        :filtered-art="''"
        :gif-from-viewer="['']"
      >
      </SaveImageToFile>
    </template>
    <template #center>
      <ColorSelection
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
  <Toolbar class="fixed bottom-0 left-0 right-0 m-2" v-if="!loggedIn">
    <template #center>
      <p class="font-bold text-xl">Login to collaborate on the canvas</p>
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
import { ref, watch, computed, onMounted, onUnmounted } from "vue";
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

//variables
const route = useRoute();
const canvas = ref<any>();
const toast = useToast();
const audioOn = ref<number>(-1);
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
const showLayers = ref<boolean>(true);
const greyscale = ref<boolean>(false);
const loggedIn = ref<boolean>(false);
let selection = ref<string[][]>([]);

const audioFiles = [
  "/src/music/In-the-hall-of-the-mountain-king.mp3",
  "/src/music/flight-of-the-bumblebee.mp3",
  "/src/music/OrchestralSuiteNo3.mp3"
];
const audioRef = ref(new Audio());

// Connection Information
const connected = ref<boolean>(false);
const groupName = ref<string>("");
let connection = new SignalR.HubConnectionBuilder()
  .withUrl("http://localhost:7154/signalhub", {
    skipNegotiation: true,
    transport: SignalR.HttpTransportType.WebSockets
  })
  .build();

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
connection.on("Send", (user: string, msg: string) => {
  console.log("Received Message", user + " " + msg);
});

// connection.on("NewMember", (newartist: Artist) => {
//   console.log("Joined Grid");
// });

connection.onclose((error) => {
  if (error) {
    toast.add({
      severity: "error",
      summary: "Error",
      detail: "You have disconnected!",
      life: 3000
    });
    connected.value = false;
  }
});

connection.on(
  "ReceivePixels",
  (layer: number, color: string, coords: Vector2[]) => {
    console.log("Receiving Pixels:", layer, color, coords);
    drawPixels(layer, color, coords);
  }
);

connection.on(
  "GroupConfig",
  (canvasSize: number, backgroundColor: string, pixels: Pixel[]) => {
    console.log("Received Group Config:", canvasSize, backgroundColor, pixels);
    art.value.pixelGrid.width = canvasSize;
    art.value.pixelGrid.height = canvasSize;
    art.value.pixelGrid.backgroundColor = backgroundColor;
    art.value.pixelGrid.grid = art.value.pixelGrid.createGrid(
      canvasSize,
      canvasSize
    );
    console.log("Canvas Size Set:", art.value.pixelGrid.width);
    replaceCanvas(pixels);
    console.log("Canvas Replaced");
    canvas.value?.drawLayers(0);
    canvas.value?.recenter();
  }
);

connection.on("BackgroundColor", (backgroundColor: string) => {
  art.value.pixelGrid.backgroundColor = backgroundColor;
});

const joinGrid = () => {
  connection
    .invoke("JoinGrid", artist.value)
    .then(() => {
      connected.value = !connected.value;
      console.log("Connected");
    })
    .catch((err) => {
      toast.add({
        severity: "error",
        summary: "Error",
        detail: err.toString().slice(err.toString().indexOf("HubException:")),
        life: 4000
      });
      connection.stop();
    });
};

const connect = () => {
  console.log("Connecting to Hub...");
  if (!loggedIn.value) {
    toast.add({
      severity: "error",
      summary: "Error",
      detail: "Please log in before collaborating!",
      life: 3000
    });
    return;
  }

  connection
    .start()
    .then(() => {
      joinGrid();
      console.log("Connected to Hub");
    })
    .catch((err) => console.error("Error connecting to Hub:", err));
};

const disconnect = () => {
  if (connected.value) {
    connection
      .invoke("LeaveGrid", artist.value)
      .then(() => {
        connection
          .stop()
          .then(() => {
            connected.value = !connected.value;
          })
          .catch((err) => console.error("Error Disconnecting:", err));
      })
      .catch((err) => console.error("Error Leaving Group:", err));
  }
};

//End of Connection Information
const cursor = ref<Cursor>(
  new Cursor(new Vector2(-1, -1), PainterTool.getDefaults()[1], 1, "000000")
);

const mouseButtonHeldDown = ref<boolean>(false);

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
  loggedIn.value = await LoginService.isLoggedIn();

  if (loggedIn.value) {
    LoginService.getCurrentUser().then((user: Artist) => {
      artist.value = user;
    });
    // connect("GridGroupName", false);
  } else {
    artist.value.id = 0;
    artist.value.name = "Guest";
    cursor.value.selectedTool = PainterTool.getDefaults()[0];
  }
  console.log("Is logged in?", loggedIn.value);

  if (route.params.id) {
    const id: number = parseInt(route.params.id as string);
    ArtAccessService.getArtById(id)
      .then((data) => {
        if (!data.artistId.includes(artist.value.id)) {
          console.log(artist.value);
          router.go(-1);
          toast.add({
            severity: "error",
            summary: "Forbid",
            detail: "Don't do that.",
            life: 3000
          });
        }
        art.value.id = data.id;
        art.value.title = data.title;
        art.value.isPublic = data.isPublic;
        art.value.pixelGrid.isGif = data.isGif;
        art.value.isGif = data.isGif;

        canvas.value?.recenter();
        art.value.pixelGrid.backgroundColor = gridCanvas.value.backgroundColor;
      })
      .catch(() => {
        toast.add({
          severity: "error",
          summary: "Error",
          detail: "You cannot edit this art",
          life: 3000
        });
        router.push("/new");
      });
  } else {
    canvas.value?.recenter();
    art.value.isGif = gridCanvas.value.isGif;
    art.value.pixelGrid.isGif = gridCanvas.value.isGif;
    art.value.pixelGrid.backgroundColor = gridCanvas.value.backgroundColor;
    art.value.pixelGrid.width = gridCanvas.value.width;
    art.value.pixelGrid.height = gridCanvas.value.height;
    tempGrid = JSON.parse(JSON.stringify(gridCanvas.value.grid));
    art.value.artistId = artistStore.artists.map((artist) => artist.id);
    art.value.artistName = artistStore.artists.map((artist) => artist.name);
  }
  connect();
});

onUnmounted(() => {
  disconnect();
  document.removeEventListener("keydown", handleKeyDown);
  window.removeEventListener("beforeunload", handleBeforeUnload);
});

function handleBeforeUnload(event: BeforeUnloadEvent) {
  artistStore.save();
}

function toggleKeybinds(disable: boolean) {
  if (disable) {
    document.removeEventListener("keydown", handleKeyDown);
  } else {
    document.addEventListener("keydown", handleKeyDown);
  }
}

watch(
  cursorPositionComputed,
  (start: Vector2, end: Vector2) => {
    drawAtCoords(getLinePixels(start, end));
  },
  { deep: true }
);

watch(mouseButtonHeldDown, async () => {
  drawAtCoords([cursor.value.position]);
});

watch(
  () => art.value.pixelGrid.backgroundColor,
  (next) => {
    changeBackgroundColor(next);
  }
);

//functions
function getLinePixels(start: Vector2, end: Vector2): Vector2[] {
  const pixels: Vector2[] = [];

  const dx = Math.abs(end.x - start.x);
  const dy = Math.abs(end.y - start.y);

  const sx = start.x < end.x ? 1 : -1;
  const sy = start.y < end.y ? 1 : -1;

  let err = dx - dy;

  let currentX = start.x;
  let currentY = start.y;

  // eslint-disable-next-line no-constant-condition
  while (true) {
    pixels.push(new Vector2(currentX, currentY));

    // Check if we have reached the end point
    if (currentX === end.x && currentY === end.y) break;

    const e2 = 2 * err;

    if (e2 > -dy) {
      err -= dy;
      currentX += sx;
    }

    if (e2 < dx) {
      err += dx;
      currentY += sy;
    }
  }

  return pixels;
}

function replaceCanvas(pixels: Pixel[]) {
  gridCanvas.value = new PixelGrid(
    art.value.pixelGrid.width,
    art.value.pixelGrid.height,
    art.value.pixelGrid.backgroundColor,
    false
  );
}

function drawPixels(layer: number, color: string, coords: Vector2[]) {
  for (const coord of coords) {
    gridCanvas.value.grid[coord.x][coord.y] = color;
    canvas.value?.updateCell(layer, coord.x, coord.y, color);
    tempGrid[coord.x][coord.y] = color;
  }
}

function sendPixels(color: string, coords: Vector2[]) {
  console.log("Attempting to send pixels...", coords[0], connected.value);
  if (connected.value) {
    console.log("Sending Pixels:", color, coords, artist.value.id);
    connection.invoke("SendGridPixels", color, coords[0], artist.value.id);
  }
}

function changeBackgroundColor(color: string) {
  if (connected.value) {
    connection.invoke("ChangeBackgroundColor", groupName.value, color);
  }
}

function drawAtCoords(coords: Vector2[]) {
  let coordinates: Vector2[] = [];

  if (
    cursor.value.selectedTool.label === "Rectangle" ||
    cursor.value.selectedTool.label === "Ellipse"
  ) {
    if (tempGrid) {
      for (let i = 0; i < gridCanvas.value.height; i++) {
        for (let j = 0; j < gridCanvas.value.width; j++) {
          gridCanvas.value.grid[i][j] = tempGrid[i][j];
          canvas.value?.updateCell(0, i, j, tempGrid[i][j]);
        }
      }
    }
  }
  coords.forEach((coord: Vector2) => {
    if (mouseButtonHeldDown.value) {
      if (cursor.value.selectedTool.label === "Brush") {
        for (let i = 0; i < cursor.value.size; i++) {
          for (let j = 0; j < cursor.value.size; j++) {
            if (
              coord.x + i >= 0 &&
              coord.x + i < gridCanvas.value.width &&
              coord.y + j >= 0 &&
              coord.y + j < gridCanvas.value.height
            ) {
              coordinates.push(new Vector2(coord.x + i, coord.y + j));
              gridCanvas.value.grid[coord.x + i][coord.y + j] =
                cursor.value.color;

              if (!gridCanvas.value.isGif) {
                canvas.value?.updateCell(
                  0,
                  coord.x + i,
                  coord.y + j,
                  cursor.value.color
                );
              }
            }
          }
        }
        sendPixels(cursor.value.color, coordinates);
      }
    }
  });
}

function setStartVector() {
  startPix.value = new Vector2(
    cursor.value.position.x,
    cursor.value.position.y
  );
  tempGrid = JSON.parse(JSON.stringify(gridCanvas.value.grid));
}
function setEndVector() {
  if (mouseButtonHeldDown.value) {
    endPix.value = new Vector2(
      cursor.value.position.x,
      cursor.value.position.y
    );
  } else {
    tempGrid = JSON.parse(JSON.stringify(gridCanvas.value.grid));
  }
}

function onMouseUp() {
  gridCanvas.value.updateGrid();
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

function toggleMusic(): void {
  if (audioOn.value != -1) {
    audioOn.value = -1;
    audioRef.value.pause();
  } else {
    audioOn.value = 1;
    audioRef.value.pause();
    audioRef.value.currentTime = 0;

    var randomIndex = Math.floor(Math.random() * audioFiles.length);
    var chosenMusic = audioFiles[randomIndex];
    audioRef.value.src = chosenMusic;
    audioRef.value.play();
  }
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
    } else if (event.ctrlKey && event.key === "s") {
      console.log("Ctrl+s was pressed.");
      event.preventDefault();
      console.log(art.value);
      saveToFile();
    }
  }
}
</script>
