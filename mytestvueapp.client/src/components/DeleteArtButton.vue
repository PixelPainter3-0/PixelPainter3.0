<template>
  <Button
    v-bind="normalizedAttrs"
    :icon="normalizedIcon"
    label="Delete Art"
    severity="danger"
    @click="onDelete"
  />

  <Dialog
    v-model:visible="visible"
    modal
    :closable="false"
    :style="{ width: '25rem' }"
    :header="`Delete ${art.title}?`"
  >
    <Message icon="pi pi-times-circle" severity="error">
      This action cannot be undone.
    </Message>

    <div class="mt-4 mb-2">Confirm the title to continue.</div>
    <InputText
      placeholder="Title"
      class="w-full"
      v-model="confirmText"
      autofocus
    />

    <template #footer>
      <Button label="Cancel" text severity="secondary" @click="visible = false" />
      <Button
        label="Confirm"
        severity="danger"
        :disabled="confirmText !== art.title"
        @click="confirmDelete"
      />
    </template>
  </Dialog>
</template>

<script setup lang="ts">
import Button from "primevue/button";
import Dialog from "primevue/dialog";
import InputText from "primevue/inputtext";
import Message from "primevue/message";
import { ref, watch, computed, useAttrs } from "vue";
import ArtAccessService from "@/services/ArtAccessService";
import { useToast } from "primevue/usetoast";
import router from "@/router";
import type Art from "@/entities/Art";

const props = defineProps<{
  art: Art;
  isAdmin: boolean;
}>();

const toast = useToast();
const visible = ref<boolean>(false);
const confirmText = ref<string>("");

const attrs = useAttrs();

const normalizedAttrs = computed(() => {
  const a: Record<string, unknown> = { ...attrs };
  if ("icon" in a) delete a.icon; // handled separately
  const t = a.title;
  if (t != null && typeof t !== "string") a.title = String(t);
  return a;
});

const normalizedIcon = computed(() => {
  const icon = (attrs as Record<string, unknown>).icon;
  return typeof icon === "string" && icon.length > 0 ? icon : "pi pi-trash";
});

function onDelete(): void {
  visible.value = true;
}

watch(visible, (open) => {
  if (open) confirmText.value = "";
});

async function confirmDelete(): Promise<void> {
  try {
    if (props.isAdmin) {
      await ArtAccessService.deleteArt(props.art.id);
    } else {
      await ArtAccessService.deleteContributingArtist(props.art.id);
    }
    visible.value = false;
    toast.add({
      severity: "success",
      summary: "Art Deleted",
      detail: "The art has been deleted successfully",
      life: 3000
    });
    router.go(-1);
  } catch {
    toast.add({
      severity: "error",
      summary: "Error",
      detail: "An error occurred while deleting the art",
      life: 3000
    });
  }
}
</script>
