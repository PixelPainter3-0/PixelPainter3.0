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

    import { useRoute } from "vue-router";


    const defaultIcon = L.icon({
        iconUrl: markerIconUrl,
        shadowUrl: markerShadowUrl,
        iconSize: [25, 41],
        iconAnchor: [12, 41],
        popupAnchor: [1, -34],
        shadowSize: [41, 41]
    })

    const route = useRoute();
    const artId = Number(route.params.id);
    //var artId = 2;

    L.Marker.prototype.options.icon = defaultIcon

    let map: L.Map
    let circle: L.Circle
    let outOfRange: L.Popup
    let inRange: L.Popup
    let artspacePolygon: L.polygon

    var points = ref<Point[]>([]);

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

                        L.marker([point.latitude, point.longitude]).addTo(map).bindPopup(`<a href='http://localhost:5173/art/${pointArt[0].id}' target="_blank">${pointArt[0].title}</a>`);

                    }
                } catch (error) {
                    console.error(`Error loading art for point ${point.id}:`, error);
                }
            }
        }
    }

    function setupArtspace() {
        console.warn("passed artId: " + artId);
        MapAccessService.getArtspaceById(1).then((artspace) => {
            if (artspace) {
                outOfRange = L.popup();
                inRange = L.popup();
                console.warn(artspace.shape);
                console.warn(stringToPolygon(artspace.shape));
                artspacePolygon = L.polygon(JSON.parse(stringToPolygon(artspace.shape))).addTo(map);
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

    
    async function handleTagArt(lat: number, lng: number) {
        console.log("Function ran before redirect " + lat + ", " + lng);
        const pointId = await MapAccessService.createPoint(lat, lng, "untitled", 1);
        console.log(pointId);
        await MapAccessService.updateArtLocation(artId, pointId);
        window.location.href = "http://localhost:5173/art/" + artId;
    }
    
    // Make globally accessible
    (window as any).handleTagArt = handleTagArt;



    function outMapClick(e: L.LeafletMouseEvent) {
        if (isPointInPolygon(e.latlng, artspacePolygon)) {
            return; // Don't show map popup if clicking inside polygon
        }
        outOfRange
            .setLatLng(e.latlng)
            .setContent("<b>Can't tag art at " +  e.latlng.lat.toFixed(4).toString() + ", " + e.latlng.lng.toFixed(4).toString() + "</b><br>Out of bounds")
            .openOn(map);
    }

    function inMapClick(e: L.LeafletMouseEvent) {
        inRange
            .setLatLng(e.latlng)
            .setContent("<b>Tag art at " + e.latlng.lat.toFixed(4).toString() + ", " + e.latlng.lng.toFixed(4).toString() +"?</b><br>" + `<a href="#" onclick="handleTagArt(${e.latlng.lat}, ${e.latlng.lng})">Confirm</a>`)
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


