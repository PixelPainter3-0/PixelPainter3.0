export default class TagService {
  public static async getAllTags(): Promise<any[]> {
    const response = await fetch("/Tag/GetAll");
    if (!response.ok) throw new Error("Failed to fetch tags");
    return await response.json();
  }

  public static async createTag(name: string): Promise<any> {
    const response = await fetch("/Tag/Create", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ name })
    });
    if (!response.ok) throw new Error("Failed to create tag");
    return await response.json();
  }

  public static async assignTagsToArt(artId: number, tagIds: number[]): Promise<any> {
    const response = await fetch(`/Tag/AssignToArt`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        artId: artId,
        tagISs: tagIds
      })
    });
    if (!response.ok) throw new Error("Failed to assign tags");
    return await response.json();
  }

  public static async getTagsForArt(artId: number): Promise<any[]> {
    const response = await fetch(`/Tag/GetTagsForArt?artId=${artId}`);
    if (!response.ok) throw new Error("Failed to fetch tags for art");
    return await response.json();
  }

  public static async removeTagFromArt(artId: number, tagId: number): Promise<any> {
    const response = await fetch(`/Tag/RemoveFromArt?artId=${artId}&tagId=${tagId}`, {
      method: "DELETE"
    });
    if (!response.ok) throw new Error("Failed to remove tag from art");
    return await response.json();
  }
}