<template>
    <div class="admin-page p-3">
        <h2 class="mb-3">Admin Panel</h2>

        <div v-if="!ready" class="p-2">Loading…</div>
        <div v-else>
            <div v-if="!isAdmin" class="p-2">
                <p>You do not have permission to view this page.</p>
            </div>
            <div v-else>

                <div class="flex align-items-center justify-content-between mt-4 mb-2">
                    <h3 class="m-0">User Administration</h3>
                    <span class="text-sm">Showing {{ userStart + 1 }}–{{ userEnd }} of {{ filteredArtists.length }}</span>
                </div>
                <div class="flex align-items-center justify-content-between gap-2 mb-3">
                    <InputText v-model="userSearch" placeholder="Search users…" class="w-full mr-2" />
                    <Dropdown v-model="userPageSize" :options="pageSizeOptions" optionLabel="label" optionValue="value"
                              class="w-10rem" />
                </div>
                <div v-if="artists.length === 0" class="p-2">No users found.</div>
                <div v-else class="grid">
                    <div v-for="a in pagedArtists" :key="a.id" class="col-6 md:col-4 lg:col-3 xl:col-3">
                        <Card class="h-full">
                            <template #title>
                                <span class="clickable breakable" title="View user profile"
                                      @click="goToUser(a.name || ('User #' + a.id))">
                                    {{ truncateText(a.name || ('User #' + a.id)) }}
                                </span>
                            </template>
                            <template #content>
                                <div class="flex align-items-center gap-2">
                                    <span class="text-sm">Admin:</span>
                                    <span class="font-medium" :class="a.isAdmin ? 'text-green-600' : 'text-red-600'">
                                        {{
                                        a.isAdmin ? 'Yes' : 'No'
                                        }}
                                    </span>
                                </div>
                            </template>

                            <template #footer>
                                <Button v-if="!a.isAdmin" size="small" icon="pi pi-user-plus" label="Grant Admin"
                                        @click="setUserAdmin(a, true)" />
                                <Button v-else size="small" severity="danger" icon="pi pi-user-minus"
                                        label="Revoke Admin" @click="setUserAdmin(a, false)" />
                            </template>

                        </Card>

                    </div>

                </div>
                <div class="flex gap-2 mt-2">
                    <Button text :disabled="userPage === 0" label="Prev" icon="pi pi-chevron-left"
                            @click="userPage = Math.max(0, userPage - 1)" />
                    <Button text :disabled="userEnd >= filteredArtists.length" label="Next" icon="pi pi-chevron-right"
                            iconPos="right" @click="userPage = userPage + 1" />
                </div>

                <div class="flex align-items-center justify-content-between mt-4 mb-2">
                    <h3 class="m-0">Tags</h3>
                    <span class="text-sm">Showing {{ tagStart + 1 }}–{{ tagEnd }} of {{ filteredTags.length }}</span>
                </div>
                <div class="flex align-items-center justify-content-between gap-2 mb-3">
                    <InputText v-model="tagSearch" placeholder="Search tags…" class="w-full mr-2" />
                    <Dropdown v-model="tagPageSize" :options="pageSizeOptions" optionLabel="label" optionValue="value"
                              class="w-10rem" />
                </div>
                <div v-if="tags.length === 0" class="p-2">No tags found.</div>
                <div v-else class="grid">
                    <div v-for="t in pagedTags" :key="t.id" class="col-6 md:col-4 lg:col-3 xl:col-3">
                        <Card class="h-full">
                            <template #title>
                                <span class="name clickable breakable" title="View tag gallery"
                                      @click="goToTag(t.name)"
                                      role="button">{{ truncateText(t.name) }}</span>
                            </template>
                            <template #content>
                                <small v-if="t.creationDate" class="text-color-secondary">
                                    Created: {{ new Date(t.creationDate as any).toLocaleDateString() }}
                                </small>
                            </template>
                            <template #footer>
                                <Button size="small" severity="danger" icon="pi pi-trash" label="Delete"
                                        @click="confirmDelete(t)" />
                            </template>
                        </Card>
                    </div>
                </div>
                <div class="flex gap-2 mt-2">
                    <Button text :disabled="tagPage === 0" label="Prev" icon="pi pi-chevron-left"
                            @click="tagPage = Math.max(0, tagPage - 1)" />
                    <Button text :disabled="tagEnd >= filteredTags.length" label="Next" icon="pi pi-chevron-right"
                            iconPos="right" @click="tagPage = tagPage + 1" />
                </div>

               <div class="flex align-items-center justify-content-between mt-4 mb-2">
                    <h3 class="m-0">Locations</h3>
                    <span class="text-sm">Showing {{ locationStart + 1 }}–{{ locationEnd }} of {{ filteredLocations.length }}</span>
                </div>
                <div class="flex align-items-center justify-content-between gap-2 mb-3">
                    <InputText v-model="locationSearch" placeholder="Search locations…" class="w-full mr-2" />
                    <Dropdown v-model="locationPageSize" :options="pageSizeOptions" optionLabel="label" optionValue="value"
                              class="w-10rem" />
                </div>
                <div v-if="locations.length === 0" class="p-2">No locations found.</div>
                <div v-else class="grid">
                    <div v-for="t in pagedLocations" :key="t.id" class="col-6 md:col-4 lg:col-3 xl:col-3">
                        <Card class="h-full">
                            <template #title>
                                <span class="name clickable breakable" title="View on map"
                                      @click="goToLocation(t)"
                                      role="button">{{ truncateText(t.title) }}</span>
                            </template>
                            <template #content>
                            </template>
                            <template #footer>
                                <Button size="small" severity="danger" icon="pi pi-trash" label="Delete"
                                        @click="removeLocation(t)" />
                            </template>
                        </Card>
                    </div>
                </div>
                <div class="flex gap-2 mt-2">
                    <Button text :disabled="locationPage === 0" label="Prev" icon="pi pi-chevron-left"
                            @click="tagPage = Math.max(0, tagPage - 1)" />
                    <Button text :disabled="locationEnd >= filteredLocations.length" label="Next" icon="pi pi-chevron-right"
                            iconPos="right" @click="locationPage = locationPage + 1" />
                </div>
                
            </div>
        </div>
        <div class="admin-bottom-spacer" aria-hidden="true"></div>
    </div>
    <ConfirmDialog />
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue';
import router from '@/router';
import Button from 'primevue/button';
import ConfirmDialog from 'primevue/confirmdialog';
import { useConfirm } from 'primevue/useconfirm';
import LoginService from '@/services/LoginService';
import TagService from '@/services/TagService';
import MapService from '@/services/MapAccessService';
import Card from 'primevue/card';
import InputText from 'primevue/inputtext';
import Dropdown from 'primevue/dropdown';
import type Artist from '@/entities/Artist';
import type Point from '@/entities/Point';

// Helper to truncate long text with an ellipsis
function truncateText(text?: string, maxChars: number = 19): string {
    const s = (text || '').trim();
    if (s.length === 0) return '';
    return s.length > maxChars ? s.slice(0, maxChars) + '…' : s;
}

type Tag = { id: number; name: string; creationDate?: string };

const isAdmin = ref<boolean>(false);
const ready = ref<boolean>(false);
const tags = ref<Tag[]>([]);
    const locations = ref<Point[]>([]);
const confirm = useConfirm();
const artists = ref<Artist[]>([]);
function goToTag(name?: string) {
    const tag = (name || '').trim();
    if (!tag) return;
    router.push({ name: 'TagGallery', params: { tag } });
}

function goToUser(name?: string) {
    const n = (name || '').trim();
    if (!n) return;
    router.push(`/accountpage/${encodeURIComponent(n)}#created_art`);
}

function goToLocation(p?: Point) {
    if (!p || !p.id) return;
    // Navigate to gallery location detail
    router.push(`/gallery/location/${encodeURIComponent(String(p.id))}`);
}

// Search + paging state
const pageSizeOptions = [
    { label: '12 per page', value: 12 },
    { label: '24 per page', value: 24 },
    { label: '48 per page', value: 48 }
];

const tagSearch = ref('');
const tagPage = ref(0);
const tagPageSize = ref(12);

const locationSearch = ref('');
const locationPage = ref(0);
const locationPageSize = ref(12);

const userSearch = ref('');
const userPage = ref(0);
const userPageSize = ref(12);

async function loadTags() {
    const list = await TagService.getAllTags();
    tags.value = (list || []).map((t: any) => ({ id: t.id, name: t.name, creationDate: t.creationDate }));
    tagPage.value = 0;
}

async function loadLocations() {
    try {
        const list = await MapService.getAllPoints();
        locations.value = (list || []).map(a => ({ ...a }));
        locationPage.value = 0;
    } catch (e) {
        console.error(e);
    }
}

async function loadArtists() {
    try {
        const list = await LoginService.GetAllArtists();
        artists.value = (list || []).map(a => ({ ...a }));
        userPage.value = 0;
    } catch (e) {
        console.error(e);
    }
}

// Debounced search watchers
let tagSearchTimer: any;
watch(tagSearch, () => {
    clearTimeout(tagSearchTimer);
    tagSearchTimer = setTimeout(() => {
        tagPage.value = 0;
    }, 300);
});

let locationSearchTimer: any;
watch(locationSearch, () => {
    clearTimeout(locationSearchTimer);
    locationSearchTimer = setTimeout(() => {
        locationPage.value = 0;
    }, 300);
});

let userSearchTimer: any;
watch(userSearch, () => {
    clearTimeout(userSearchTimer);
    userSearchTimer = setTimeout(() => {
        userPage.value = 0;
    }, 300);
});

// Computed filtered + paged collections
const filteredTags = computed(() => {
    const q = tagSearch.value.trim().toLowerCase();
    if (!q) return tags.value;
    return tags.value.filter(t => (t.name || '').toLowerCase().includes(q));
});
const tagStart = computed(() => Math.min(tagPage.value * tagPageSize.value, filteredTags.value.length));
const tagEnd = computed(() => Math.min(tagStart.value + tagPageSize.value, filteredTags.value.length));
const pagedTags = computed(() => filteredTags.value.slice(tagStart.value, tagEnd.value));

const filteredLocations = computed(() => {
    const q = locationSearch.value.trim().toLowerCase();
    if (!q) return locations.value;
    return locations.value.filter(a => ((a.title || a.title || '').toLowerCase().includes(q)));
});
const locationStart = computed(() => Math.min(locationPage.value * locationPageSize.value, filteredLocations.value.length));
const locationEnd = computed(() => Math.min(locationStart.value + locationPageSize.value, filteredLocations.value.length));
const pagedLocations = computed(() => filteredLocations.value.slice(locationStart.value, locationEnd.value));

const filteredArtists = computed(() => {
    const q = userSearch.value.trim().toLowerCase();
    if (!q) return artists.value;
    return artists.value.filter(a => ((a.name || a.name || '').toLowerCase().includes(q)));
});
const userStart = computed(() => Math.min(userPage.value * userPageSize.value, filteredArtists.value.length));
const userEnd = computed(() => Math.min(userStart.value + userPageSize.value, filteredArtists.value.length));
const pagedArtists = computed(() => filteredArtists.value.slice(userStart.value, userEnd.value));

function confirmDelete(tag: Tag) {
    confirm.require({
        message: `Delete tag "${tag.name}"?`,
        header: 'Confirm',
        icon: 'pi pi-exclamation-triangle',
        rejectLabel: 'Cancel',
        acceptLabel: 'Delete',
        accept: async () => {
            try {
                await TagService.deleteTag(tag.id);
                await loadTags();
            } catch (e) {
                console.error(e);
                alert('Failed to delete tag');
            }
        }
    });
    }

function removeLocation(location: Point) {
    confirm.require({
        message: `Delete location "${location.title}"?`,
        header: 'Confirm',
        icon: 'pi pi-exclamation-triangle',
        rejectLabel: 'Cancel',
        acceptLabel: 'Delete',
        accept: async () => {
            try {
                await MapService.deleteLocation(location.id);
                await loadLocations();
            } catch (e) {
                console.error(e);
                alert('Failed to delete location');
            }
        }
    });
}

onMounted(async () => {
    try {
        isAdmin.value = await LoginService.getIsAdmin();
        if (!isAdmin.value) {
            // hard redirect if not admin
            router.replace('/');
            ready.value = true;
            return;
        }
        await Promise.all([loadTags(), loadLocations(), loadArtists()]);
    } finally {
        ready.value = true;
    }
});

async function setUserAdmin(a: Artist, makeAdmin: boolean) {
    const ok = await LoginService.setAdmin(a.id as any, makeAdmin);
    if (ok) {
        a.isAdmin = makeAdmin as any;
    } else {
        alert('Failed to update admin status');
    }
}
</script>

<style scoped>
.admin-page {
    max-width: 800px;
    margin: 0 auto;
    padding-bottom: 0;
}

.admin-bottom-spacer {
    height: 4rem;
    width: 100%;
}

.tag-list {
    list-style: none;
    padding: 0;
    margin: 0;
}

.tag-row {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 0.5rem 0;
    border-bottom: 1px solid var(--header-border, rgba(0, 0, 0, 0.08));
}

.name {
    font-weight: 500;
}

/* Uniform section spacing */
.admin-page h3 { margin-top: 1.25rem; }
.admin-page .grid { margin-top: .5rem; }
.admin-page .flex.gap-2.mt-2 { margin-top: .75rem; }

/* Clickable text */
.clickable { cursor: pointer; }
.clickable:hover { text-decoration: underline; }

/* Allow breaking long, space-less names safely */
.breakable {
    overflow-wrap: anywhere;
    word-break: break-word;
}
</style>
