<template>
  <div class="m-4">
    <div class="flex gap-4 justify-content-center">
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
              label="Notification Settings"
              :severity="route.hash == '#notification_settings' ? 'primary' : 'secondary'"
              @click="changeHash('#notification_settings')"
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
      </div>

  <div v-if="route.hash == '#notification_settings'">
  <h2>Notification Settings</h2>
  <Card>
    <template #content>
      <h3>Enable or Disable Notification Types</h3>
      <div class="align-items-stretch flex">
        <p class="w-10">Art Liked Notification</p>
        <Button
          class="block m-2 flex"
          :label="notifLikes ? 'Enabled' : 'Disabled'"
          :severity="notifLikes ? 'primary' : 'secondary'"
          :icon="notifLikes ? 'pi pi-check' : 'pi pi-times'"
          @click="notifLikes = !notifLikes"
        />
      </div>
      <div class="align-items-stretch flex">
        <p class="w-10">Art Disliked Notification</p>
        <Button
          class="block m-2 flex"
          :label="notifDislikes ? 'Enabled' : 'Disabled'"
          :severity="notifDislikes ? 'primary' : 'secondary'"
          :icon="notifDislikes ? 'pi pi-check' : 'pi pi-times'"
          @click="notifDislikes = !notifDislikes"
        />
      </div>
      <div class="align-items-stretch flex">
        <p class="w-10">Comment Notification</p>
        <Button
          class="block m-2 flex"
          :label="notifComments ? 'Enabled' : 'Disabled'"
          :severity="notifComments ? 'primary' : 'secondary'"
          :icon="notifComments ? 'pi pi-check' : 'pi pi-times'"
          @click="notifComments = !notifComments"
        />
      </div>

      <div class="align-items-stretch flex">
        <p class="w-10">Reply Notification</p>
        <Button
          class="block m-2 flex"
          :label="notifReplies ? 'Enabled' : 'Disabled'"
          :severity="notifReplies ? 'primary' : 'secondary'"
          :icon="notifReplies ? 'pi pi-check' : 'pi pi-times'"
          @click="notifReplies = !notifReplies"
        />
      </div>
      <div class="align-items-stretch flex">
        <p class="w-10">Comment Liked Notification</p>
        <Button
          class="block m-2 flex"
          :label="notifCommentLikes ? 'Enabled' : 'Disabled'"
          :severity="notifCommentLikes ? 'primary' : 'secondary'"
          :icon="notifCommentLikes ? 'pi pi-check' : 'pi pi-times'"
          @click="notifCommentLikes = !notifCommentLikes"
        />
      </div>
        <div class="align-items-stretch flex">
        <p class="w-10">Comment Disliked Notification</p>
        <Button
          class="block m-2 flex"
          :label="notifCommentDislikes ? 'Enabled' : 'Disabled'"
          :severity="notifCommentDislikes ? 'primary' : 'secondary'"
          :icon="notifCommentDislikes ? 'pi pi-check' : 'pi pi-times'"
          @click="notifCommentDislikes = !notifCommentDislikes"
        />
      </div>

      <!-- ✅ Save Button -->
      <div class="flex justify-content-center mt-3">
        <Button
          label="Save Notification Settings"
          icon="pi pi-save"
          severity="success"
          @click="saveNotifications"
        />
      </div>
    </template>
  </Card>
</div>


      <div v-if="route.hash == '#created_art'">
        <h2>{{ createdArtHeading }}</h2>
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
import { useRoute } from "vue-router";
import { useToast } from "primevue/usetoast";

import LoginService from "@/services/LoginService";
import ArtAccessService from "@/services/ArtAccessService";
import NotificationService from "@/services/NotificationService";

import type Art from "@/entities/Art";
import Artist from "@/entities/Artist";

// PrimeVue components used in template
//import router from "@/router";
import Card from "primevue/card";
import Button from "primevue/button";
import InputText from "primevue/inputtext";
import Avatar from "primevue/avatar";
import Message from "primevue/message";
// Child component
import ArtCard from "@/components/Gallery/ArtCard.vue";
//import { createBuilderStatusReporter } from "typescript";

const toast = useToast();
const route = useRoute();

const isAdmin = ref<boolean>(false);
const curArtist = ref<Artist>(new Artist());
const curUser = ref<Artist>(new Artist());
const pageStatus = ref<string>("");

const isEditing = ref<boolean>(false);
const newUsername = ref<string>("");



const notifLikes = ref<boolean>(true);
const notifComments = ref<boolean>(true);
const notifReplies = ref<boolean>(true);
const notifDislikes = ref<boolean>(true);
const notifCommentLikes = ref<boolean>(true);
const notifCommentDislikes = ref<boolean>(true);


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
    console.log(artistInfo);
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
  if (!["#settings", "notifications_settings", "#created_art", "#liked_art"].includes(route.hash)) {
    changeHash("#settings");
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
  updateNotifications();
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

function updateNotifications(): void {
  console.log(curArtist.value);
  const value = curArtist.value.notificationsEnabled ?? 0;
  notifLikes.value = (value & 1) !== 0;
  notifComments.value = (value & 2) !== 0;
  notifReplies.value = (value & 4) !== 0;
  notifCommentLikes.value = (value & 8) !== 0;
  notifCommentDislikes.value = (value & 16) !== 0;
  notifDislikes.value = (value & 32) !==0;
}
function computeNotificationsEnabled(): number {
  let value = 0;
  if (notifLikes.value) value += 1;
  if (notifComments.value) value += 2;
  if (notifReplies.value) value += 4;
  if (notifCommentLikes.value) value += 8;
  if (notifCommentDislikes.value) value += 16;
  if (notifDislikes.value) value +=   32;
  return value;
}

async function saveNotifications(): Promise<void> {
  const newValue = computeNotificationsEnabled(); 
  const success = await NotificationService.updateNotificationsEnabled(curArtist.value.id, newValue);
  if (success) {
    curArtist.value.notificationsEnabled = newValue;
    toast.add({
      severity: "success",
      summary: "Success",
      detail: "Notification settings updated",
      life: 2000
    });
  } else {
    toast.add({
      severity: "error",
      summary: "Error",
      detail: "Failed to update notifications",
      life: 2000
    });
  }
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
.profile-card {
  min-width: 16rem;
}

@media (max-width: 640px) {
  .profile-card {
    width: 100%;
  }
}
</style>  