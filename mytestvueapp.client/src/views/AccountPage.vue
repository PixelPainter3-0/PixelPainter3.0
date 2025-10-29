<template>
  <div class="m-4">
    <div class="flex gap-4 justify-content-center">
      <Card class="h-fit profile-card">
        <template #content>
          <Avatar icon="pi pi-user" class="mr-2" size="xlarge" shape="circle" />
          <div class="text-3xl p-font-bold">{{ 
          // @ts-ignore
           curArtist.name
           }}</div>
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

      <div v-if="route.hash == '#settings'">
        <h2>Account Settings</h2>
        <Card>
          <template #content>
            <div class="mb-4">
              <label for="username">Username</label>
              <div class="flex gap-1 flex-row align-items-center">
                <div class="flex flex-column gap-2">
                  <InputText
                    :disabled="!isEditing"
                    class="mr-1"
                    v-model="newUsername"
                    variant="filled"
                  />
                </div>
                <Button v-if="!isEditing"
                  severity="secondary"
                  rounded
                  icon="pi pi-pencil"
                  @click="isEditing = true"
                />
                <span v-else class="">
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
                :label="errorMessage"
                v-if="errorMessage != ''"
                severity="error"
                variant="simple"
                size="small"
                class="mt-2"
              />
            </div>
            <div class="align-items-stretch flex">
              <Button
                class="block m-2"
                label="logout"
                icon="pi pi-sign-out"
                @click="logout()"
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
        <div class="flex flex-column gap-2">
          <h2>Current Page Status: {{ pageStatus }}</h2>
          <Button
            class="block m-2"
            label=" Click to change page status"
            icon="pi pi-eye"
            @click="privateSwitchChange()"
          />
        </div>
      </div>
      <div v-if="route.hash == '#created_art'">
        <h2>My Art</h2>
        <div class="shrink-limit flex flex-wrap">
          <ArtCard
            v-for="(art, idx) in myArt"
            :key="art.id"
            :art="art"
            :size="10"
            :position="idx"
          />
        </div>
      </div>
      <div v-if="route.hash == '#liked_art'">
        <h2>Liked Art</h2>
        <div class="shrink-limit flex flex-wrap">
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
import LoginService from "@/services/LoginService";
import ArtAccessService from "@/services/ArtAccessService";
import router from "@/router";
import { useToast } from "primevue/usetoast";
import Card from "primevue/card";
import Button from "primevue/button";
import InputText from "primevue/inputtext";
import Artist from "@/entities/Artist";
import Avatar from "primevue/avatar";
import Message from "primevue/message";
import { useRoute } from "vue-router";
import type Art from "@/entities/Art";
import ArtCard from "@/components/Gallery/ArtCard.vue";

const toast = useToast();
const route = useRoute();

const artist = ref<Artist>(new Artist());
const isEditing = ref<boolean>(false);
const newUsername = ref<string>("");
const isAdmin = ref<boolean>(false);
const curArtist = ref<Artist>(new Artist());
const curUser = ref<Artist>(new Artist());
const pageStatus = ref<string>("");

var myArt = ref<Art[]>([]);
var likedArt = ref<Art[]>([]);

const canEdit = computed<boolean>(() => {
  return curUser.value.id === curArtist.value.id || isAdmin.value;
});

async function loadArtistData(artistName: string): Promise<void> {
  if (!artistName) return;

  myArt.value = [];
  likedArt.value = [];

  try {
    const artistInfo = await LoginService.GetArtistByName(artistName);
    curArtist.value = artistInfo;
    newUsername.value = artistInfo.name;

    if (artistInfo.privateProfile) {
      if (curUser.value.id !== artistInfo.id && !isAdmin.value) {
        toast.add({
          severity: "error",
          summary: "Access Denied",
          detail: "Account page is declared as private",
          life: 3000
        });
        router.go(-1);
        return;
      }
      pageStatus.value = "Private";
    } else {
      pageStatus.value = "Public";
    }

    const [created, liked] = await Promise.all([
      ArtAccessService.getAllArtByUserID(artistInfo.id),
      ArtAccessService.getLikedArt(artistInfo.id)
    ]);

    myArt.value = (created ?? []).map((a: any) => ({
      ...a,
      tags: a?.tags ?? [],
      artistName: a?.artistName ?? [],
      title: a?.title ?? "",
      numComments: a?.numComments ?? 0,
      numLikes: a?.numLikes ?? 0,
      numDislikes: a?.numDislikes ?? 0
    }));

    likedArt.value = (liked ?? []).map((a: any) => ({
      ...a,
      tags: a?.tags ?? [],
      artistName: a?.artistName ?? [],
      title: a?.title ?? "",
      numComments: a?.numComments ?? 0,
      numLikes: a?.numLikes ?? 0,
      numDislikes: a?.numDislikes ?? 0
    }));
  } catch {
    toast.add({
      severity: "error",
      summary: "Error",
      detail: "Failed to load artist data",
      life: 3000
    });
  }
}

onMounted(async () => {
  // Try to get current user, but do not block anonymous visitors
  try {
    const user = await LoginService.getCurrentUser();
    if (user && user.id !== 0) {
      curUser.value = user;
      isAdmin.value = !!user.isAdmin;
    } else {
      curUser.value = new Artist(); // anonymous
      curUser.value.id = 0 as any;
      isAdmin.value = false;
    }
  } catch {
    // treat as anonymous
    curUser.value = new Artist();
    curUser.value.id = 0 as any;
    isAdmin.value = false;
  }

  // Default to created art if no/invalid hash
  if (!['#settings', '#created_art', '#liked_art'].includes(route.hash)) {
    changeHash('#created_art');
  }

  await loadArtistData(String(route.params.artist ?? ""));
});

// Refetch on artist change
watch(
  () => route.params.artist,
  async (newArtist) => {
    await loadArtistData(String(newArtist ?? ""));
  }
);

// Prevent navigating to settings if viewer cannot edit
watch(
  () => route.hash,
  (h) => {
    if (h === '#settings' && !canEdit.value) {
      toast.add({
        severity: "info",
        summary: "Read-only",
        detail: "Login to manage this account.",
        life: 2500
      });
      changeHash('#created_art');
    }
  }
);

async function logout(): Promise<void> {
  LoginService.logout().then(() => {
    window.location.replace(`/`);
    toast.add({
      severity: "success",
      summary: "Success",
      detail: "User logged out",
      life: 3000
    });
  });
}

function cancelEdit(): void {
  isEditing.value = false;
  newUsername.value = curUser.value.name;
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
  LoginService.updateUsername(newUsername.value)
    .then((success) => {
      if (success) {
        toast.add({
          severity: "success",
          summary: "Success",
          detail: "Username successfully changed",
          life: 3000
        });
        curArtist.value.name = newUsername.value;
        artist.value.name = newUsername.value;
        isEditing.value = false;
      } else {
        toast.add({
          severity: "error",
          summary: "Error",
          detail: "Username is already taken. Try another",
          life: 3000
        });
      }
    })
    .catch(() => {
      toast.add({
        severity: "error",
        summary: "Error",
        detail: "An error occurred. Please try again later",
        life: 3000
      });
    });
}

async function confirmDelete(): Promise<void> {
  LoginService.deleteArtist(curArtist.value.id)
    .then(() => {
      window.location.href = "/";
      toast.add({
        severity: "success",
        summary: "User Deleted",
        detail: "The User has been deleted successfully",
        life: 3000
      });
    })
    .catch(() => {
      toast.add({
        severity: "error",
        summary: "Error",
        detail:
          "error deleting user, please make sure you have spelt it correctly",
        life: 3000
      });
    });
}

function changeHash(hash: string): void {
  if (hash === '#settings' && !canEdit.value) {
    toast.add({
      severity: "info",
      summary: "Read-only",
      detail: "Login to manage this account.",
      life: 2500
    });
    return;
  }
  window.location.hash = hash;
}

async function privateSwitchChange() {
  await LoginService.privateSwitchChange(curArtist.value.id).then(() => {
    if (pageStatus.value === "Private") {
      pageStatus.value = "Public";
    } else {
      pageStatus.value = "Private";
    }
    curArtist.value.privateProfile = !curArtist.value.privateProfile;
  });
}
</script>

<style scoped>
/* Ensure the left profile card has a sensible minimum width */
.profile-card {
  min-width: 16rem; /* ~256px */
}

/* Stack and fill on small screens */
@media (max-width: 640px) {
  .profile-card {
    width: 100%;
  }
}
</style>