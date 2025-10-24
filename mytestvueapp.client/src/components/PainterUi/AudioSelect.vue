<template>
    <FloatingCard
    position="right"
    header="Audio Settings"
    width="22rem"
    button-icon="pi pi-volume-up"
    button-label=""
    button-title="Audio Controls"
    :default-open="false"
    >
    <div class="flex items-center gap-3 p-3">
      <!-- Mute Button -->
      <Button
        :icon="volume > 0 ? 'pi pi-volume-up' : 'pi pi-volume-off'"
        :severity="volume > 0 ? 'primary' : 'secondary'"
        class="p-2 rounded-full"
        @click="$emit('toggle-mute')"
      />

      <!-- Horizontal Slider -->
      <Slider
        v-model="internalVolume"
        :min="0"
        :max="100"
        class="flex-1"
        @change="emitVolume"
      />
    </div>
<p class="text-xs text-gray-400 text-center mt-2">Volume: {{ internalVolume }}%</p>
  </FloatingCard>
</template>

<script setup lang="ts">
import { ref, watch } from "vue";
import Button from "primevue/button";
import Slider from "primevue/slider";
import FloatingCard from "./FloatingCard.vue";

const props = defineProps({
  volume: { type: Number, required: true },
});

const emit = defineEmits(["update:volume", "toggle-mute"]);

const internalVolume = ref(props.volume);

// Keep slider synced with parent
watch(
  () => props.volume,
  (val) => {
    if (val !== internalVolume.value) internalVolume.value = val;
    console.log("AudioSelect.vue -> incoming volume:", val);
  }
);

const emitVolume = () => {
  emit("update:volume", internalVolume.value);
};
</script>