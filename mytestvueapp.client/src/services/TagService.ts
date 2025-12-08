export default class TagService {
  private static normalizeTag(tag: any) {
    if (tag == null) return tag;
    if (tag.tagId != null && tag.id == null) tag.id = tag.tagId;
    if (tag.Id != null && tag.id == null) tag.id = tag.Id;
    if (typeof tag.id === "string") {
      const n = Number(tag.id);
      if (!Number.isNaN(n)) tag.id = n;
    }
    return tag;
  }

  public static async getAllTags(): Promise<any[]> {
    const response = await fetch("/tag/GetAll");
    if (!response.ok) throw new Error("Failed to fetch tags");
    const data = await response.json();
    return Array.isArray(data) ? data.map(TagService.normalizeTag) : [];
  }

  public static async createTag(name: string): Promise<any> {
    const response = await fetch("/tag/Create", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ name })
    });
    if (!response.ok) {
      let t = "";
      try { t = await response.text(); } catch {}
      throw new Error("Failed to create tag: " + t);
    }
    let tag = TagService.normalizeTag(await response.json());

    // Fallback: server returned id 0 or missing, resolve by refetching
    if (!tag.id || tag.id <= 0) {
      try {
        const all = await TagService.getAllTags();
        const found = all.find(t => (t.name || "").toLowerCase() === name.toLowerCase());
        if (found && found.id && found.id > 0) {
          tag = found;
        }
      } catch {
        // ignore fallback errors
      }
    }
    return tag;
  }

  public static async assignTagsToArt(artId: number, tagIds: number[]): Promise<void> {
    const cleanIds = [...new Set(tagIds.filter(id => Number.isFinite(id) && id > 0))];
    if (!cleanIds.length) return;

    let response = await fetch(`/tag/AssignToArt`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ artId, tagIds: cleanIds })
    });

    if (!response.ok && [400,404,415,500].includes(response.status)) {
      const qs = `artId=${artId}&` + cleanIds.map(id => `tagIds=${id}`).join("&");
      response = await fetch(`/tag/AssignToArt?${qs}`, { method: "POST" });
    }

    if (!response.ok) {
      let errText = "";
      try { errText = await response.text(); } catch {}
      throw new Error(`Failed to assign tags (${response.status}) ${errText}`);
    }
  }

  public static async getTagsForArt(artId: number): Promise<any[]> {
    const response = await fetch(`/tag/GetTagsForArt?artId=${artId}`);
    if (!response.ok) throw new Error("Failed to fetch tags for art");
    const data = await response.json();
    return Array.isArray(data) ? data.map(TagService.normalizeTag) : [];
  }

  public static async removeTagFromArt(artId: number, tagId: number): Promise<void> {
    const response = await fetch(`/tag/RemoveFromArt?artId=${artId}&tagId=${tagId}`, { method: "DELETE" });
    if (!response.ok) throw new Error("Failed to remove tag from art");
  }

  public static async deleteTag(tagId: number): Promise<void> {
    const response = await fetch(`/tag/DeleteTag?tagId=${tagId}`, { method: "DELETE" });
    if (!response.ok) {
      let t = "";
      try { t = await response.text(); } catch {}
      throw new Error(`Failed to delete tag (${response.status}) ${t}`);
    }
  }
}