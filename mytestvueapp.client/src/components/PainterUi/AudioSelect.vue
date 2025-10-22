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
    <div class="gap-3 p-3">
      <!-- Horizontal row: Small square button + Slider -->
      <div class="flex items-center gap-4 w-full">
        <!-- Mute / Unmute Button as small square -->
        <Button
          :icon="volume > 0 ? 'pi pi-volume-up' : 'pi pi-volume-off'"
          :severity="volume > 0 ? 'primary' : 'secondary'"
          class="w-10 h-10 p-0 flex items-center justify-center"
          @click="toggleMute"
        />

        <!-- Horizontal Slider -->
        <Slider
          v-model="volume"
          :min="0"
          :max="100"
          class="w-full"
          @input="$emit('update:volume', volume)"
        />
      </div>

      <!-- Volume display -->
      <p class="text-sm text-gray-400 text-center">
        Volume: {{ volume }}%
      </p>
    </div>
  </FloatingCard>
</template>

<script setup lang="ts">
import { defineProps, defineEmits, computed } from "vue";
import Button from "primevue/button";
import Slider from "primevue/slider";
import FloatingCard from "./FloatingCard.vue";

const props = defineProps<{
  volume: number
}>();
const emit = defineEmits(["update:volume"]);

const toggleMute = () => {
  const newVolume = props.volume > 0 ? 0 : 50;
  emit("update:volume", newVolume);
};
</script>