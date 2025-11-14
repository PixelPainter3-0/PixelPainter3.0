<template>
  <div style="margin: 1rem .2rem 2rem .2rem;">
    <div class="flex gap-4 justify-content-center account-main">
      <Card class="h-fit profile-card">
        <template #content>
          <Avatar icon="pi pi-user" class="mr-2" size="xlarge" shape="circle" />
          <div class="text-3xl p-font-bold">{{ curArtist.name }}</div>

          <div class="flex mt-4 p-2 gap-2 flex-column">
            <Button
              :disabled="!canEdit"
              label="Account Settings"
              :severity="route.hash == '#settings' ? 'primary' : 'secondary'"
              @click="changeHash('#settings')"
            />
            <Button
              label="Creator's Art"
              :severity="route.hash == '#created_art' ? 'primary' : 'secondary'"
              @click="changeHash('#created_art')"
            />
            <Button
              label="Liked Art"
              :severity="route.hash == '#liked_art' ? 'primary' : 'secondary'"
              @click="changeHash('#liked_art')"
            />
          </div>
        </template>
      </Card>

      <div v-if="route.hash == '#settings'" class="account-content">
        <h2>Account Settings</h2>
        <Card>
          <template #content>
            <div class="mb-4">
              <label for="username">Username</label>
              <div class="flex gap-1 flex-row align-items-center">
                <div class="flex flex-column gap-2">
                  <InputText
                    id="username"
                    :disabled="!isEditing"
                    class="mr-1"
                    v-model="newUsername"
                    variant="filled"
                  />
                </div>
                <Button
                  v-if="!isEditing"
                  severity="secondary"
                  rounded
                  icon="pi pi-pencil"
                  @click="isEditing = true"
                />
                <span v-else>
                  <Button
                    severity="danger"
                    text
                    rounded
                    icon="pi pi-times"
                    @click="cancelEdit()"
                  />
                  <Button
                    severity="success"
                    text
                    rounded
                    icon="pi pi-check"
                    @click="updateUsername()"
                    :disabled="errorMessage != ''"
                  />
                </span>
              </div>
              <Message
                v-if="errorMessage != ''"
                :label="errorMessage"
                severity="error"
                variant="simple"
                size="small"
                class="mt-2"
              />
            </div>
            <div class="align-items-stretch flex">
              <Button
                class="block m-2"
                label="Logout"
                icon="pi pi-sign-out"
                @click="logout()
                "
              />
              <Button
                class="block m-2"
                label="Delete Artist"
                severity="danger"
                @click="confirmDelete()"
              />
            </div>
          </template>
        </Card>
      </div>

      <div v-if="route.hash == '#created_art'" class="account-content">
        <h2>{{ createdArtHeading }}</h2>
        <!-- Desktop: grid of cards. Mobile (<=1000px): vertical scrolling feed like ImageViewer -->
        <div class="art-grid">
          <ArtCard
            v-for="(art, idx) in myArt"
            :key="art.id"
            :art="art"
            :size="10"
            :position="idx"
          />
        </div>
      </div>

      <div v-if="route.hash == '#liked_art'" class="account-content">
        <h2>Liked Art</h2>
        <div class="art-grid">
          <ArtCard
            v-for="(art, idx) in likedArt"
            :key="art.id"
            :art="art"
            :size="10"
            :position="idx"
          />
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, computed, watch } from "vue";
import { useRoute } from "vue-router";
import { useToast } from "primevue/usetoast";

import LoginService from "@/services/LoginService";
import ArtAccessService from "@/services/ArtAccessService";

import type Art from "@/entities/Art";
import Artist from "@/entities/Artist";

// PrimeVue components used in template
import Card from "primevue/card";
import Button from "primevue/button";
import InputText from "primevue/inputtext";
import Avatar from "primevue/avatar";
import Message from "primevue/message";
// Child component
import ArtCard from "@/components/Gallery/ArtCard.vue";

const toast = useToast();
const route = useRoute();

const isAdmin = ref<boolean>(false);
const curArtist = ref<Artist>(new Artist());
const curUser = ref<Artist>(new Artist());
const pageStatus = ref<string>("");

const isEditing = ref<boolean>(false);
const newUsername = ref<string>("");

const myArt = ref<Art[]>([]);
const likedArt = ref<Art[]>([]);

const canEdit = computed<boolean>(
  () => curUser.value.id === curArtist.value.id || isAdmin.value
);

function normalizeTags(a: any) {
  let tags = a?.tags;
  if (Array.isArray(tags) && tags.length && typeof tags[0] === "string") {
    tags = tags.map((t: string) => ({ name: t }));
  }
  if (!tags && Array.isArray(a?.artTags)) {
    tags = a.artTags.map((at: any) => at?.tag ?? at).filter(Boolean);
  }
  if (!tags && Array.isArray(a?.tagList)) {
    tags = a.tagList.map((t: any) => (typeof t === "string" ? { name: t } : t));
  }
  return Array.isArray(tags) ? tags : [];
}

async function loadArtistData(artistName: string): Promise<void> {
  if (!artistName) return;

  myArt.value = [];
  likedArt.value = [];

  try {
    const artistInfo = await LoginService.GetArtistByName(artistName);
    curArtist.value = artistInfo;
    newUsername.value = artistInfo.name ?? "";

    pageStatus.value = artistInfo.privateProfile ? "Private" : "Public";

    const [createdRes, likedRes] = await Promise.allSettled([
      ArtAccessService.getAllArtByUserID(artistInfo.id),
      ArtAccessService.getLikedArt(artistInfo.id),
    ]);

    if (createdRes.status === "fulfilled") {
      myArt.value = (createdRes.value ?? []).map((a: any) => ({
        ...a,
        tags: normalizeTags(a),
        artistName: a?.artistName ?? [],
        title: a?.title ?? "",
        numComments: a?.numComments ?? 0,
        numLikes: a?.numLikes ?? 0,
        numDislikes: a?.numDislikes ?? 0,
      }));
    } else {
      toast.add({
        severity: "warn",
        summary: "Art",
        detail: "Failed to load creator's art.",
        life: 2500,
      });
    }

    if (likedRes.status === "fulfilled") {
      likedArt.value = (likedRes.value ?? []).map((a: any) => ({
        ...a,
        tags: normalizeTags(a),
        artistName: a?.artistName ?? [],
        title: a?.title ?? "",
        numComments: a?.numComments ?? 0,
        numLikes: a?.numLikes ?? 0,
        numDislikes: a?.numDislikes ?? 0,
      }));
    } else {
      likedArt.value = [];
    }
  } catch (e) {
    toast.add({
      severity: "error",
      summary: "Error",
      detail: "Failed to load artist data",
      life: 3000,
    });
  }
}

onMounted(async () => {
  // Default tab if none/invalid
  if (!["#settings", "#created_art", "#liked_art"].includes(route.hash)) {
    changeHash("#created_art");
  }

  // Try to get current user, but allow anonymous
  try {
    const user = await LoginService.getCurrentUser();
    if (user && user.id !== 0) {
      curUser.value = user;
      isAdmin.value = !!(user as any).isAdmin;
    }
  } catch {
    // ignore, treat as anonymous
  }

  await loadArtistData(String(route.params.artist ?? ""));
});

watch(
  () => route.params.artist,
  async (newArtist) => {
    await loadArtistData(String(newArtist ?? ""));
  }
);

function changeHash(hash: string): void {
  if (hash === "#settings" && !canEdit.value) {
    toast.add({
      severity: "info",
      summary: "Read-only",
      detail: "Login to manage this account.",
      life: 2500,
    });
    return;
  }
  window.location.hash = hash;
}

function cancelEdit(): void {
  isEditing.value = false;
  newUsername.value = curArtist.value.name ?? "";
}

const errorMessage = computed<string>(() => {
  if (newUsername.value.length > 16) {
    return "Username is too long. Max of 16 characters.";
  }
  if (newUsername.value.length < 4) {
    return "Username is too short. Min of 4 characters.";
  }
  return "";
});

async function updateUsername(): Promise<void> {
  try {
    const success = await LoginService.updateUsername(newUsername.value);
    if (success) {
      curArtist.value.name = newUsername.value;
      isEditing.value = false;
      toast.add({
        severity: "success",
        summary: "Updated",
        detail: "Username changed",
        life: 2500,
      });
    } else {
      toast.add({
        severity: "error",
        summary: "Error",
        detail: "Username is already taken.",
        life: 3000,
      });
    }
  } catch {
    toast.add({
      severity: "error",
      summary: "Error",
      detail: "An error occurred. Please try again later.",
      life: 3000,
    });
  }
}

async function logout(): Promise<void> {
  try {
    await LoginService.logout();
    window.location.replace(`/`);
    toast.add({
      severity: "success",
      summary: "Success",
      detail: "User logged out",
      life: 3000,
    });
  } catch {
    // swallow
  }
}

async function confirmDelete(): Promise<void> {
  try {
    // @ts-ignore
    await LoginService.deleteArtist(curArtist.value.id);
    window.location.href = "/";
    toast.add({
      severity: "success",
      summary: "User Deleted",
      detail: "The user has been deleted successfully",
      life: 3000,
    });
  } catch {
    toast.add({
      severity: "error",
      summary: "Error",
      detail: "Error deleting user.",
      life: 3000,
    });
  }
}

// Computed properties
const isOwnProfile = computed(() => curUser.value.id === curArtist.value.id);
// Heading that matches the viewed account
const createdArtHeading = computed(() =>
  isOwnProfile.value ? "My Art" : `${curArtist.value.name ?? "Artist"}'s Art`
);
</script>

<style scoped>
/* Responsive layout modeled after ImageViewer */

/* Main row: keep side-by-side on desktop, prevent horizontal scroll */
.account-main {
  display: flex;
  flex-direction: column;
  align-items: stretch;
  gap: 1rem;
  justify-content: flex-start;
  width: 100%;
  box-sizing: border-box;
  padding: 0 1rem;
  overflow-x: hidden;

  /* Align the profile p-card with the art container on desktop */
  max-width: 1200px; /* adjust to match art container width */
  margin: 0 auto;    /* centers the whole account area */
}

/* Make section headings line up with the art container */
.account-content > h2 {
  max-width: 1200px; /* keep in sync with .account-main/.art-grid */
  margin-left: auto;
  margin-right: auto;
}

/* Mobile: keep existing centering behavior */
@media (max-width: 1000px) {
  .account-content > h2 {
    max-width: 100%;
    margin-left: 0;
    margin-right: 0;
  }
}

/* Left column (profile card) */
.profile-card {
  flex: 0 0 auto;
  align-self: center;
  width: 300px;
  margin: 0 auto; /* ensure horizontal centering */
  margin-top: var(--profile-card-top-gap, 1.0rem); /* gap between header bar and profile card */
}

/* Limit the actual PrimeVue card width and keep it centered */
.profile-card :deep(.p-card) {
  max-width: 26rem; /* ← adjust this value as needed */
  width: 100%;
  margin: 0 auto;   /* keep p-card centered within wrapper */
  box-sizing: border-box;
}

/* Mobile: center the p-card */
@media (max-width: 1000px) {
  .profile-card {
    align-self: center;
  }
  .profile-card :deep(.p-card) {
    margin: 0 auto;   /* center horizontally */
    max-width: 22rem; /* optional tighter cap on small screens */
  }
}

/* Center PrimeVue card content for the profile card */
.profile-card :deep(.p-card-content) {
  display: flex;
  flex-direction: column;
  align-items: center;
  text-align: center;
  gap: 0.5rem;
}

/* Right column (page content area) */
.account-content {
  flex: 1 1 60rem;             /* take remaining space */
  min-width: 0;
  max-width: 100%;
  overflow-x: hidden;          /* contain any internal overflow */
}

/* Optionally ensure other cards' content doesn't overflow and stays full width */
.account-content :deep(.p-card-content) {
  box-sizing: border-box;
  width: 100%;
}

/* Art grids: ensure wrapping and no overflow */
.shrink-limit {
  width: 100%;
  box-sizing: border-box;
  gap: 0.75rem;
  overflow-x: hidden;
}
.shrink-limit > * { min-width: 0; }

/* Responsive grid for account art sections
   - Desktop: rows and columns
   - <=1300px: slightly narrower columns (matches ImageViewer tablet behavior)
   - <=1000px: switch to single-column scrolling feed (matches HeaderBar mobile) */
.art-grid {
  --art-card-min: 260px; /* adjust min card width here if you want larger/smaller columns */
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(var(--art-card-min), 1fr));
  grid-auto-rows: auto;
  gap: 1rem;
  width: 100%;
  box-sizing: border-box;
  overflow-x: hidden; /* never cause horizontal scroll */

  /* Center the art container; adjust max-width to control centered width */
  max-width: 1200px;   /* ← change this value as needed */
  margin-left: auto;
  margin-right: auto;
}
.art-grid > * {
  min-width: 0; /* allow grid items to shrink without overflow */
}

/* Prevent the 4→5 column jump near ~1202px.
   Force exactly 4 columns on desktop; mobile (≤1000px) still switches to a feed. */
@media (min-width: 1001px) {
  .art-grid {
    grid-template-columns: repeat(4, minmax(0, 1fr)) !important;
    grid-auto-flow: row;
  }
}

/* Keep media inside cards responsive */
.art-grid :deep(img),
.art-grid :deep(canvas),
.art-grid :deep(video) {
  max-width: 100%;
  height: auto;
  display: block;
}

/* Tablet/large laptop */
@media (max-width: 1300px) {
  .art-grid { --art-card-min: 220px; }
}

/* Mobile breakpoint (matches ImageViewer/HeaderBar) -> vertical feed */
@media (max-width: 1000px) {
  .art-grid {
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
    max-width: 100%; /* full width on mobile */
  }
  .art-grid > * { width: 100%; }
}

/* Extra small safety: images/media never exceed container */
.account-content :deep(img),
.account-content :deep(canvas),
.account-content :deep(video) {
  max-width: 100%;
  height: auto;
  display: block;
  
}

/* Limit width of primary PrimeVue buttons in this view to prevent overly wide labels */
.p-button {
  max-width: 13rem;
  min-width: 13rem;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  margin: 0 auto; 

}

/* Ensure these containers never become their own scroll areas */
.account-content { overflow-y: visible; }
.art-grid { overflow-y: visible; }

/* Mobile fix: make the whole page a single scrolling column */
@media (max-width: 1000px) {
  .account-main {
    display: block;                 /* drop flex to avoid nested scroll on children */
    overflow-y: visible !important;
  }

  /* Profile card: keep a max width on mobile and center it */
  .profile-card {
    flex: none !important;          
    width: 100% !important;
    max-width: 20rem !important;    /* <-- adjust this value to change mobile profile card max width */
    margin: 0 auto !important;      /* centers the card */
    height: auto !important;
    max-height: none !important;
    overflow-y: visible !important; /* page handles scrolling */
  }

  /* Content area: full width below the card */
  .account-content {
    flex: none !important;
    width: 100% !important;
    max-width: 100% !important;
    height: auto !important;
    max-height: none !important;
    overflow-y: visible !important;
  }

  .art-grid {
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
    max-width: 100%;
    height: auto !important;
    max-height: none !important;
    overflow-y: visible !important;
  }
}
</style>

<!-- 12 -->