<template>
  <h1 class="flex align-items-center gap-3 ml-4">Map</h1>
  <div id="map" ref="mapContainer" style="height: 400px; width: 100%"></div>
</template>

<script setup lang="ts">
  import { onMounted, ref } from 'vue'
  import L from 'leaflet'
  import 'leaflet/dist/leaflet.css'

  onMounted(async () => {
    var map = L.map('map').setView([44.87313, -91.92544], 15);

      
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      attribution: '&copy; OpenStreetMap contributors'
      }).addTo(map)

  });

</script>

<style scoped>
#map { 
  height: 180px; 
}
.lCard {
  width: 80vw;
  border: 1px solid #ddd;
  border-radius: 10px;
  box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
  overflow: hidden;
  transition: transform 0.3s ease;
  padding: 3px;
  padding-left: 5px;
  padding-right: 5px;
  margin-left: 20px;
  margin-right: 20px;
  margin-bottom: 5px;
}
.lCardV {
  width: 80vw;
  border: 1px solid #ddd;
  border-radius: 10px;
  box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
  overflow: hidden;
  transition: transform 0.3s ease;
  background-color: #d3d3d3;
  color: #7f7f7f;
  padding: 3px;
  padding-left: 5px;
  padding-right: 5px;
  margin-left: 20px;
  margin-right: 20px;
  margin-bottom: 5px;
}

.lCard:hover {
  background-color: #d3d3d3;
}

.dCard {
  width: 80vw;
  border: 1px solid #ddd;
  border-radius: 10px;
  box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
  overflow: hidden;
  transition: transform 0.3s ease;
  padding: 3px;
  padding-left: 5px;
  padding-right: 5px;
  margin-left: 20px;
  margin-right: 20px;
  margin-bottom: 5px;
}

.dCardV {
  width: 80vw;
  border: 1px solid #ddd;
  border-radius: 10px;
  box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
  overflow: hidden;
  background-color: #7f7f7f;
  color: #d3d3d3;
  transition: transform 0.3s ease;
  padding: 3px;
  padding-left: 5px;
  padding-right: 5px;
  margin-left: 20px;
  margin-right: 20px;
  margin-bottom: 5px;
}

.dCard:hover {
  background-color: #7f7f7f;
}

.upside-down {
  transform: rotate(180deg); /* Flips the icon upside down */
  display: inline-block;
}
.SadText {
  margin: 40px;
}
</style>
