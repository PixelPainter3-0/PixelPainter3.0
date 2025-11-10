<template>
  <h1 class="flex align-items-center gap-3 ml-4">Map</h1>
  <div id="map" ref="mapContainer"></div>
</template>

<script setup lang="ts">
  import { onMounted, ref } from 'vue'
  import L from 'leaflet'
  import 'leaflet/dist/leaflet.css'
  import Point from "../entities/Point";

  import markerIconUrl from 'leaflet/dist/images/marker-icon.png'
  import markerShadowUrl from 'leaflet/dist/images/marker-shadow.png'

  import MapAccessService from "../services/MapAccessService";

  const defaultIcon = L.icon({
    iconUrl: markerIconUrl,
    shadowUrl: markerShadowUrl,
    iconSize: [25, 41],
    iconAnchor: [12, 41],
    popupAnchor: [1, -34],
    shadowSize: [41, 41]
  })

  L.Marker.prototype.options.icon = defaultIcon 

  let map: L.Map
  let circle: L.Circle
  let outOfRange: L.Popup
  let inRange: L.Popup
  let artspacePolygon: L.Polygon

  var points = ref<Point[]>([]);
  //var point = ref<Point>;


    async function setupPoints() {
        const points = await MapAccessService.getAllPoints();

        if (points && points.length > 0) {
            for (const point of points) {
                try {
                    const pointArt = await MapAccessService.getArtByPoint(point.id);

                    if (!pointArt || pointArt.length === 0) {
                        console.log(`No art found for point ${point.id}`);
                    } else {
                        console.log(`Art found for point ${point.id}:`, pointArt);

                        L.marker([point.latitude, point.longitude]).addTo(map).bindPopup(`<a href='https://pixelpainter.app/art/${pointArt[0].id}' target="_blank">${pointArt[0].title}</a>`);

                    }
                } catch (error) {
                    console.error(`Error loading art for point ${point.id}:`, error);
                }
            }
        }
    }

   function setupArtspace() {
       console.warn("*");
       MapAccessService.getArtspaceById(1).then((artspace) => {
           if (artspace) {
               outOfRange = L.popup();
               inRange = L.popup();
               console.warn(artspace.shape);
               console.warn(stringToPolygon(artspace.shape));
               artspacePolygon = L.polygon(JSON.parse(stringToPolygon(artspace.shape))).addTo(map);
               //artspacePolygon = L.polygon([[44.867474, -91.929726], [44.86741, -91.928766], [44.866539, -91.927999], [44.866547, -91.924624], [44.871056, -91.92456], [44.871063, -91.923342], [44.875127, -91.923342], [44.875112, -91.923949], [44.874705, -91.923997], [44.874713, -91.925247], [44.874975, -91.925252], [44.875017, -91.925901], [44.875593, -91.925896], [44.875608, -91.927183], [44.876042, -91.927167], [44.876087, -91.928353], [44.876513, -91.928363], [44.876522, -91.92971], [44.875857, -91.929678], [44.875629, -91.93074], [44.873823, -91.930804], [44.873778, -91.931673], [44.87296, -91.93177], [44.872945, -91.930279], [44.873188, -91.930311], [44.873204, -91.929699]]).addTo(map);
               artspacePolygon.on('click', inMapClick);
               map.on('click', outMapClick);
           }
           else {
               console.warn("No artspaces with that id");
           }
       }).catch((error) => {
           console.error("Error fetching artspace:", error);
       });
    }

    //AI written then tweaked
    function stringToPolygon(wkt: string): string {
        // Remove "POLYGON ((" and "))"
        const cleaned = wkt.replace("POLYGON ((", "").replace("))", "");

        // Split into coordinate pairs
        const pairs = cleaned.split(",").map(pair => {
            const [latStr, lngStr] = pair.trim().split(" ");
            const lat = parseFloat(latStr);
            const lng = parseFloat(lngStr);
            return [lat, lng];
        });

        // Return Leaflet polygon string
        return `${JSON.stringify(pairs)}`;
    }

    //AI written and directed
    function isPointInPolygon(point: L.LatLng, polygon: L.Polygon): boolean {
        // Step 1: Bounding box check
        const bounds = polygon.getBounds();
        if (!bounds.contains(point)) {
            return false; // Definitely outside
        }

        // Step 2: Ray-casting algorithm
        const latlngs = polygon.getLatLngs()[0] as L.LatLng[];
        const x = point.lat, y = point.lng;

        let inside = false;
        for (let i = 0, j = latlngs.length - 1; i < latlngs.length; j = i++) {
            const xi = latlngs[i].lat, yi = latlngs[i].lng;
            const xj = latlngs[j].lat, yj = latlngs[j].lng;

            const intersect = ((yi > y) !== (yj > y)) &&
                (x < (xj - xi) * (y - yi) / (yj - yi + 0.0000001) + xi);
            if (intersect) inside = !inside;
        }

        return inside;
    }

  function outMapClick(e: L.LeafletMouseEvent) {
      if (isPointInPolygon(e.latlng, artspacePolygon)) {
    return; // Don't show map popup if clicking inside polygon
  }
  outOfRange
      .setLatLng(e.latlng)
      .setContent("<b>Out of bounds at " + e.latlng.lat.toFixed(4).toString() + ", " + e.latlng.lng.toFixed(4).toString() + "</b>")
      .openOn(map);
  }

  function inMapClick(e: L.LeafletMouseEvent) {
  inRange
      .setLatLng(e.latlng)
      .setContent("<b>No art at " + e.latlng.lat.toFixed(4).toString() + ", " + e.latlng.lng.toFixed(4).toString() + "</b>")
      .openOn(map);
  }

  onMounted(async () => {   
    map = L.map('map').setView([44.87313, -91.92544], 15);

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      attribution: '&copy; OpenStreetMap contributors'
    }).addTo(map)

    setupPoints();
    setupArtspace();
  });
</script>

<style scoped>
#map {
    height: calc(85vh - 60px); /* Adjust 60px based on your header height */
    width: 100%;
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


