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

    //circle = L.circle([44.87313, -91.92544], {
    //  color: 'red',
    //  fillColor: '#f03',
    //  fillOpacity: 0,
    //  radius: 1500
    //}).addTo(map);

    // polygon = L.polygon([
    //   [44.872348, -91.938679],
    //   [44.87209, -91.937987],
    //   [44.871778, -91.937896],
    //   [44.871383, -91.937386],
    //   [44.871318, -91.936491],
    //   [44.870763, -91.936394],
    //   [44.870995, -91.93575],
    //   [44.870653, -91.935444],
    //   [44.870493, -91.935841],
    //   [44.870018, -91.935477],
    //   [44.869927, -91.93509],
    //   [44.8696, -91.935144],
    //   [44.869436, -91.934409],
    //   [44.869018, -91.934527],
    //   [44.868577, -91.933803],
    //   [44.868375, -91.933969],
    //   [44.867961, -91.933873],
    //   [44.867847, -91.933267],
    //   [44.86779, -91.933562],
    //   [44.867398, -91.933186],
    //   [44.867459, -91.932934],
    //   [44.867242, -91.932848],
    //   [44.867417, -91.93251],
    //   [44.867216, -91.932494],
    //   [44.867128, -91.932682],
    //   [44.866596, -91.932011],
    //   [44.866539, -91.930354],
    //   [44.86663, -91.929742],
    //   [44.866403, -91.928723],
    //   [44.866122, -91.928036],
    //   [44.865749, -91.927629],
    //   [44.86565, -91.926438],
    //   [44.865414, -91.925719],
    //   [44.865468, -91.924635],
    //   [44.865174, -91.923761],
    //   [44.865376, -91.923482],
    //   [44.865129, -91.923289],
    //   [44.865106, -91.922838],
    //   [44.865258, -91.921991],
    //   [44.864586, -91.919582],
    //   [44.878674, -91.919523],
    //   [44.876972, -91.922854],
    //   [44.87701, -91.925676],
    //   [44.878279, -91.928036],
    //   [44.87961, -91.928583],
    //   [44.880613, -91.928154],
    //   [44.881731, -91.928358],
    //   [44.883183, -91.92795],
    //   [44.883358, -91.929528],
    //   [44.882813, -91.933519],
    //   [44.882438, -91.935042],
    //   [44.88145, -91.936083],
    //   [44.880689, -91.936319],
    //   [44.878713, -91.936244],
    //   [44.876044, -91.937059],
    //   [44.873383, -91.937521]
    // ]).addTo(map);

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


