import Friends from "../entities/Friends";

export default class FriendService {
  
  // Get the logged-in user's friends
  public static async getArtistFriends(): Promise<Friends[]> {
    try {
      const response = await fetch(`/friends/GetArtistFriends`);

      if (!response.ok) {
        throw new Error("Problem getting artist friends");
      }

      return await response.json() as Friends[];
    } catch (error) {
      console.error("Error getting friends:", error);
      return [];
    }
  }

  // Add a new friend (artistId2 is other user ID)
  public static async insertFriends(artistId: number): Promise<boolean> {
    try {
      const response = await fetch(`/friends/InsertFriends?artistId=${artistId}`, {
        method: "POST",
      });

      console.log("Response ok: " + response.ok);
      return response.ok;
    } catch (error) {
      console.error("Error inserting friends:", error);
      return false;
    }
  }

  // Remove a friend
  public static async removeFriends(artistId: number): Promise<boolean> {
    try {
      const response = await fetch(`/friends/RemoveFriends?artistId=${artistId}`, {
        method: "DELETE",
      });

      return response.ok;
    } catch (error) {
      console.error("Error removing friends:", error);
      return false;
    }
  }
}