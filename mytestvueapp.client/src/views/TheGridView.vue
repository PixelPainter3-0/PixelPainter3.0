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
      <BrushSelection 
        v-model="cursor.selectedTool" 
        :isGrid = "true"
      />
      <Button
        icon="pi pi-expand"
        class="mr-2"
        severity="secondary"
        label=""
        title="Recenter"
        @click="canvas?.recenter()"
      />
      <HelpPopUp :isGrid = "true" />
    </template>
    <template #end>
      <Button
        :icon="audioOn != -1 ? 'pi pi-volume-up' : 'pi pi-volume-off'"
        :severity="audioOn != -1 ? 'primary' : 'secondary'"
        label=""
        class="mr-2"
        @click="toggleMusic()"
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
const gridCanvas = ref(new PixelGrid(
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

// Connection Information
const connected = ref<boolean>(false);
const groupName = ref<string>("");
let connection = new SignalR.HubConnectionBuilder()
  .withUrl("https://localhost:7154/signalhub", {
    skipNegotiation: true,
    transport: SignalR.HttpTransportType.WebSockets
  })
  .build();

const audioFiles = [
  "/src/music/In-the-hall-of-the-mountain-king.mp3",
  "/src/music/flight-of-the-bumblebee.mp3",
  "/src/music/OrchestralSuiteNo3.mp3"
];
const audioRef = ref(new Audio());

connection.on("Send", (user: string, msg: string) => {
  console.log("Received Message", user + " " + msg);
});

connection.on("NewMember", (newartist: Artist) => {
  if (!art.value.artistId.includes(newartist.id)) {
    art.value.artistId.push(newartist.id);
    art.value.artistName.push(newartist.name);
    artistStore.addArtist(newartist);
  }
});

connection.on("Members", (artists: Artist[]) => {
  art.value.artistId = [];
  art.value.artistName = [];
  artistStore.clearStorage();
  artistStore.empty();
  artists.forEach((artist) => {
    if (!art.value.artistId.includes(artist.id)) {
      art.value.artistId.push(artist.id);
      art.value.artistName.push(artist.name);
      artistStore.addArtist(artist);
    }
  });
});

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
    drawPixels(layer, color, coords);
  }
);

connection.on(
  "GroupConfig",
  (canvasSize: number, backgroundColor: string, pixels: Pixel[][]) => {

    art.value.pixelGrid.width = canvasSize;
    art.value.pixelGrid.height = canvasSize;
    art.value.pixelGrid.backgroundColor = backgroundColor;
    art.value.pixelGrid.grid = art.value.pixelGrid.createGrid(
      canvasSize,
      canvasSize
    );
    replaceCanvas(pixels);

    canvas.value?.drawLayers(0);
    canvas.value?.recenter();
  }
);

connection.on("BackgroundColor", (backgroundColor: string) => {
  art.value.pixelGrid.backgroundColor = backgroundColor;
});

const createGroup = (groupName: string) => {
  let grids = gridCanvas.value;
  connection
    .invoke(
      "CreateGroup",
      groupName,
      artist.value,
      artistStore.artists,
      grids,
      gridCanvas.value.width,
      gridCanvas.value.backgroundColor
    )
    .then(() => {
      connected.value = !connected.value;
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

const joinGroup = (groupName: string) => {
  connection
    .invoke("JoinGroup", groupName, artist.value)
    .then(() => {
      connected.value = !connected.value;
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

const connect = (groupname: string, newGroup: boolean) => {
  if (!loggedIn.value) {
    toast.add({
      severity: "error",
      summary: "Error",
      detail: "Please log in before collaborating!",
      life: 3000
    });
    return;
  }

  groupName.value = groupname;
  if (art.value.artistId[0] == 0 || art.value.artistId.length == 0) {
    art.value.artistId = [artist.value.id];
    art.value.artistName = [artist.value.name];
    artistStore.addArtist(artist.value);
  }

  connection
    .start()
    .then(() => {
      if (newGroup) {
        createGroup(groupname);
      } else {
        joinGroup(groupname);
      }
    })
    .catch((err) => console.error("Error connecting to Hub:", err));
};

const disconnect = () => {
  if (connected.value) {
    connection
      .invoke("LeaveGroup", groupName.value, artist.value)
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
        art.value.pixelGrid.backgroundColor =
          gridCanvas.value.backgroundColor;
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
  }else {
    canvas.value?.recenter();
    art.value.isGif = gridCanvas.value.isGif;
    art.value.pixelGrid.isGif = gridCanvas.value.isGif;
    art.value.pixelGrid.backgroundColor =
      gridCanvas.value.backgroundColor;
    art.value.pixelGrid.width = gridCanvas.value.width;
    art.value.pixelGrid.height = gridCanvas.value.height;
    tempGrid = JSON.parse(JSON.stringify(gridCanvas.value.grid));
    art.value.artistId = artistStore.artists.map((artist) => artist.id);
    art.value.artistName = artistStore.artists.map((artist) => artist.name);
  }
});

onUnmounted(() => {
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
    if (cursor.value.selectedTool.label === "Rectangle") {
      if (mouseButtonHeldDown.value) {
        setEndVector();
        drawAtCoords(getRectanglePixels(startPix.value, endPix.value));
      }
    } else if (cursor.value.selectedTool.label === "Ellipse") {
      if (mouseButtonHeldDown.value) {
        setEndVector();
        drawAtCoords(getEllipsePixels(startPix.value, endPix.value));
      }
    } else if (cursor.value.selectedTool.label === "Select") {
      if (mouseButtonHeldDown.value) {
        setEndVector();
          selection = ref<string[][]>(getSelectPixels(startPix.value, endPix.value));
      }
    } else {
      drawAtCoords(getLinePixels(start, end));
    }
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

function replaceCanvas(pixels: Pixel[][]) {
  for (let l = 0; l < pixels.length; l++) {
    gridCanvas.value = 
      new PixelGrid(
        art.value.pixelGrid.width,
        art.value.pixelGrid.height,
        art.value.pixelGrid.backgroundColor,
        false
      )
  }
}

function drawPixels(layer: number, color: string, coords: Vector2[]) {
  for (const coord of coords) {
    gridCanvas.value.grid[coord.x][coord.y] = color;
    canvas.value?.updateCell(layer, coord.x, coord.y, color);
    tempGrid[coord.x][coord.y] = color;
  }
}

function sendPixels(layer: number, color: string, coords: Vector2[]) {
  if (connected.value) {
    connection.invoke("SendPixels", groupName.value, layer, color, coords);
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
              gridCanvas.value.grid[coord.x + i][
                coord.y + j
              ] = cursor.value.color;

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
        sendPixels(0, cursor.value.color, coordinates);
      } else if (cursor.value.selectedTool.label === "Eraser") {
        for (let i = 0; i < cursor.value.size; i++) {
          for (let j = 0; j < cursor.value.size; j++) {
            if (
              coord.x + i >= 0 &&
              coord.x + i < gridCanvas.value.width &&
              coord.y + j >= 0 &&
              coord.y + j < gridCanvas.value.height
            ) {
              if (art.value.pixelGrid.backgroundColor != null) {
                coordinates.push(new Vector2(coord.x + i, coord.y + j));
                gridCanvas.value.grid[coord.x + i][
                  coord.y + j
                ] = "empty";
                canvas.value?.updateCell(
                  0,
                  coord.x + i,
                  coord.y + j,
                  "empty"
                );
              }
            }
          }
        }
        sendPixels(0, "empty", coordinates);
      } else if (
        coord.x >= 0 &&
        coord.x < gridCanvas.value.width &&
        coord.y >= 0 &&
        coord.y < gridCanvas.value.height
      ) {
        if (cursor.value.selectedTool.label === "Pipette") {
          let tmp = gridCanvas.value.grid[coord.x][coord.y];
          if (tmp === "empty") {
            cursor.value.color = art.value.pixelGrid.backgroundColor;
          } else {
            cursor.value.color = tmp;
          }
        } else if (cursor.value.selectedTool.label === "Bucket") {
          if (
            gridCanvas.value.grid[coord.x][coord.y] !=
            cursor.value.color
          ) {
            coordinates = fill(
              cursor.value.position.x,
              cursor.value.position.y
            );
            sendPixels(0, cursor.value.color, coordinates);
          }
        } else if (
          cursor.value.selectedTool.label === "Rectangle" ||
          cursor.value.selectedTool.label === "Ellipse"
        ) {
          gridCanvas.value.grid[coord.x][coord.y] =
            cursor.value.color;

          canvas.value?.updateCell(
            0,
            coord.x,
            coord.y,
            cursor.value.color
          );
        }
      }
    }
  });
}

function fill(
  x: number,
  y: number,
  color: string = cursor.value.color
): Vector2[] {
  let vectors: Vector2[] = [];
  if (y >= 0 && y < gridCanvas.value.height) {
    const oldColor = gridCanvas.value.grid[x][y];
    gridCanvas.value.grid[x][y] = color;

    canvas.value?.updateCell(0, x, y, color);

    vectors.push(new Vector2(x, y));
    if ("empty" !== color) {
      if (x + 1 < gridCanvas.value.width) {
        if (gridCanvas.value.grid[x + 1][y] === oldColor) {
          vectors = vectors.concat(fill(x + 1, y, color));
        }
      }
      if (y + 1 < gridCanvas.value.height) {
        if (gridCanvas.value.grid[x][y + 1] === oldColor) {
          vectors = vectors.concat(fill(x, y + 1, color));
        }
      }
      if (x - 1 >= 0) {
        if (gridCanvas.value.grid[x - 1][y] === oldColor) {
          vectors = vectors.concat(fill(x - 1, y, color));
        }
      }
      if (y - 1 >= 0) {
        if (gridCanvas.value.grid[x][y - 1] === oldColor) {
          vectors = vectors.concat(fill(x, y - 1, color));
        }
      }
    }
  }

  return vectors;
}

//returns array array of color strings
function getSelectPixels(start: Vector2, end: Vector2): string[][] {
  let outArray: string[][] = [];
  let leftBound = Math.min(start.x, end.x);
  let rightBound = Math.max(start.x, end.x);
  let lowerBound = Math.min(start.y, end.y);
  let upperBound = Math.max(start.y, end.y);

  let height = upperBound - lowerBound + 1;
  let width = rightBound - leftBound + 1;

  for (let i = 0; i < height; i++) {
    outArray[i] = []; // initialize the row?
    for (let j = 0; j < width; j++) {
        outArray[i][j] = gridCanvas.value.grid[lowerBound + i][leftBound + j];
    }
  }
  return outArray;
}

function getRectanglePixels(start: Vector2, end: Vector2): Vector2[] {
  let coords: Vector2[] = [];
  let leftBound = Math.min(start.x, end.x);
  let rightBound = Math.max(start.x, end.x);
  let lowerBound = Math.min(start.y, end.y);
  let upperBound = Math.max(start.y, end.y);

  for (let i = 0; i < cursor.value.size; i++) {
    if (
      leftBound + i <= rightBound &&
      rightBound - i >= leftBound &&
      upperBound - i >= lowerBound &&
      lowerBound + i <= upperBound
    ) {
      coords = coords.concat(
        calculateRectangle(
          new Vector2(leftBound + i, lowerBound + i),
          new Vector2(rightBound - i, upperBound - i)
        )
      );
    }
  }

  return coords;
}

function calculateRectangle(start: Vector2, end: Vector2): Vector2[] {
  const coords: Vector2[] = [];

  let boundary = art.value.pixelGrid.height;

  let stepX = start.x;
  while (stepX != end.x) {
    if (stepX >= 0 && stepX < boundary) {
      if (start.y >= 0 && start.y < boundary)
        coords.push(new Vector2(stepX, start.y));
      if (end.y >= 0 && end.y < boundary)
        coords.push(new Vector2(stepX, end.y));
    }

    if (stepX < end.x) stepX++;
    else if (stepX > end.x) stepX--;
  }

  let stepY = start.y;
  while (stepY != end.y) {
    if (stepY >= 0 && stepY < boundary) {
      if (start.x >= 0 && start.x < boundary)
        coords.push(new Vector2(start.x, stepY));
      if (end.x >= 0 && end.x < boundary)
        coords.push(new Vector2(end.x, stepY));
    }

    if (stepY < end.y) stepY++;
    else if (stepY > end.y) stepY--;
  }

  if (end.x >= 0 && end.x < boundary && end.y >= 0 && end.y < boundary) {
    coords.push(end);
  }
  return coords;
}

function getEllipsePixels(start: Vector2, end: Vector2): Vector2[] {
  let coords: Vector2[] = [];

  let leftBound = Math.min(start.x, end.x);
  let rightBound = Math.max(start.x, end.x);
  let lowerBound = Math.min(start.y, end.y);
  let upperBound = Math.max(start.y, end.y);

  for (let i = 0; i < cursor.value.size; i++) {
    if (
      leftBound + i <= rightBound &&
      rightBound - i >= leftBound &&
      upperBound - i >= lowerBound &&
      lowerBound + i <= upperBound
    ) {
      coords = coords.concat(
        calculateEllipse(
          new Vector2(leftBound + i, lowerBound + i),
          new Vector2(rightBound - i, upperBound - i)
        )
      );
    }
  }

  return coords;
}

function calculateEllipse(start: Vector2, end: Vector2): Vector2[] {
  const coords: Vector2[] = [];
  const boundary = art.value.pixelGrid.height;

  function inBounds(x: number, y: number): boolean {
    return x >= 0 && x < boundary && y >= 0 && y < boundary;
  }

  if (start.x == end.x && start.y == end.y && inBounds(start.x, start.y)) {
    coords.push(start);
    return coords;
  }
  let leftBound = Math.min(start.x, end.x);
  let rightBound = Math.max(start.x, end.x);
  let lowerBound = Math.min(start.y, end.y);
  let upperBound = Math.max(start.y, end.y);

  let xOffset = rightBound - leftBound;
  let yOffset = upperBound - lowerBound;

  let center = new Vector2(leftBound + xOffset / 2, lowerBound + yOffset / 2);

  let a = Math.max(xOffset, yOffset) / 2; //Major Axis length
  let b = Math.min(xOffset, yOffset) / 2; //Minor Axis length

  if (xOffset > yOffset) {
    // Major Axis is Horrizontal
    for (let i = leftBound; i <= rightBound; i++) {
      let yP = Math.round(ellipseXtoY(center, a, b, i));
      let yN = center.y - (yP - center.y);
      if (inBounds(i, yP)) coords.push(new Vector2(i, yP));
      if (inBounds(i, yN)) coords.push(new Vector2(i, yN));
    }
    for (let i = lowerBound; i < upperBound; i++) {
      let xP = Math.round(ellipseYtoX(center, b, a, i));
      let xN = center.x - (xP - center.x);
      if (inBounds(xP, i)) coords.push(new Vector2(xP, i));
      if (inBounds(xN, i)) coords.push(new Vector2(xN, i));
    }
  } else {
    // Major Axis is vertical
    for (let i = lowerBound; i <= upperBound; i++) {
      let xP = Math.round(ellipseYtoX(center, a, b, i));
      let xN = center.x - (xP - center.x);
      if (inBounds(xP, i)) coords.push(new Vector2(xP, i));
      if (inBounds(xN, i)) coords.push(new Vector2(xN, i));
    }
    for (let i = leftBound; i < rightBound; i++) {
      let yP = Math.round(ellipseXtoY(center, b, a, i));
      let yN = center.y - (yP - center.y);
      if (inBounds(i, yP)) coords.push(new Vector2(i, yP));
      if (inBounds(i, yN)) coords.push(new Vector2(i, yN));
    }
  }
  return coords;
}

function ellipseXtoY(
  center: Vector2,
  majorAxis: number,
  minorAxis: number,
  x: number
): number {
  let yPow = Math.pow((x - center.x) / majorAxis, 2);
  let ySqrt = Math.sqrt(1 - yPow);
  let y = minorAxis * ySqrt + center.y;
  return y;
}

function ellipseYtoX(
  center: Vector2,
  majorAxis: number,
  minorAxis: number,
  y: number
): number {
  let xPow = Math.pow((y - center.y) / majorAxis, 2);
  let xSqrt = Math.sqrt(1 - xPow);
  let x = minorAxis * xSqrt + center.x;
  return x;
}

function setStartVector() {
  startPix.value = new Vector2(
    cursor.value.position.x,
    cursor.value.position.y
  );
  tempGrid = JSON.parse(
    JSON.stringify(gridCanvas.value.grid)
  );
}
function setEndVector() {
  if (mouseButtonHeldDown.value) {
    endPix.value = new Vector2(
      cursor.value.position.x,
      cursor.value.position.y
    );
  } else {
    tempGrid = JSON.parse(
      JSON.stringify(gridCanvas.value.grid)
    );
  }
}

function onMouseUp() {
  if (cursor.value.selectedTool.label == "Rectangle") {
    sendPixels(
      0,
      cursor.value.color,
      getRectanglePixels(startPix.value, endPix.value)
    );
  } else if (cursor.value.selectedTool.label == "Ellipse") {
    sendPixels(
      0,
      cursor.value.color,
      getEllipsePixels(startPix.value, endPix.value)
    );
  } else if (cursor.value.selectedTool.label == "Select") {
    selection.value = getSelectPixels(startPix.value, endPix.value);
  }
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