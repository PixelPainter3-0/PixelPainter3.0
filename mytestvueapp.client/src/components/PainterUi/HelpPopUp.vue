<template>
  <FloatingCard
    position="right"
    header="Helpful Hotkeys"
    button-icon="pi pi-question-circle"
    button-label=""
    button-title="Hotkeys tooltip"
    width=""
    :default-open="false"
  >
  
  <div>
    <p> CTRL + S ) Upload canvas </p>
    <p> CTRL + D ) Download canvas</p>
    <p> CTRL + Z ) Undo last stroke</p>
    <p> CTRL + Y ) Redo last undo</p>
    <p> 1-9 ) Color selection for the brush</p>
    <p> Q ) Decrease brush size by 1</p>
    <p> W ) Increase brush size by 1</p>
  </div>

  </FloatingCard>
</template>

<script setup lang="ts">
import FloatingCard from "./FloatingCard.vue";

</script>
<style>
.p-dialog.p-component {
  margin-bottom: 75px !important;
}
</style>
