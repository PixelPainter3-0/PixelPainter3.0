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
    <p> ctrl + s ) Upload canvas </p>
    <p> ctrl + d ) Download canvas</p>
    <p> ctrl + z ) Undo last stroke</p>
    <p> ctrl + y ) Redo last undo</p>
    <p> 1-9 ) Color selection for the brush</p>
    <p> q ) Decrease brush size by 1</p>
    <p> w ) Increase brush size by 1</p>
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
