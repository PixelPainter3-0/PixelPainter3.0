<template>
  <div class="absolute w-full bottom-50">
    <Card class="flex align-items-center justify-content-start w-30rem m-auto">
      <template #title>Connect to "The Grid"</template>
      <template #content>
        <label>Must be logged in to connect.</label>
        <br />
        <br />
        <Button class="bottom-0 bg-primary flex align-items-center w-full"
          rounded
          label="Connect"
          icon="pi pi-wifi"
          :disabled="loggedIn"
          @click="goToGrid()"
        ></Button>
      </template>
    </Card>
  </div>
</template>
<script setup lang="ts">
import Button from "primevue/button";
import Card from "primevue/card";
import { onMounted, ref } from "vue";
import router from "@/router";
import LoginService from "@/services/LoginService";

const loggedIn = ref<boolean>(false);

function goToGrid(): void {
  window.location.replace("/login/Login");
}

onMounted(async () => {
  loggedIn.value = await LoginService.isLoggedIn();
  if(loggedIn.value){
    router.push("/thegrid");
  }
});

</script>
<style>
.p-colorpicker-preview {
  width: 7rem !important;
  height: 2rem !important;
  border: solid 1px !important;
}
</style>
