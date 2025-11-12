import Notification from "../entities/Notification";

export default class NotificationService {
  public static async getNotifications(artistId: number): Promise<Notification[]> {
  const allNotifications: Notification[] = [];
  try {
    const response = await fetch(
      `/notification/GetNotificationsForArtist?artistId=${artistId}`
    );

    if (!response.ok) {
      throw new Error("Problem getting notifications");
    }

    const data = await response.json();

    for (const newNotification of data) {
      allNotifications.push(newNotification as Notification);
    }

    return allNotifications;
  } catch (error) {
    console.error("Error getting notifications:", error);
    return allNotifications;
  }
}
  public static async markCommentViewed(commentId: number): Promise<boolean> {
    try {
      const response = await fetch(`/notification/MarkCommentViewed`, {
        method: "POST", 
        body: JSON.stringify(commentId), 
        headers: { "Content-Type": "application/json" }});
      if(response.ok){
        return true
      } else {
        throw new Error("Problem marking notification viewed in database");
      }
    } catch (error){
      console.error(error);
      return false;
    }
  }
  public static async markLikeViewed(artId: number, artistId: number): Promise<boolean> {
    try {
      const response = await fetch(`/notification/MarkLikeViewed`, {
        method: "POST", 
        body: JSON.stringify({"artId": artId, "artistId": artistId}), 
        headers: { "Content-Type": "application/json" }
      });
      if(response.ok){
        return true
      } else {
        throw new Error("Problem marking notification viewed in database");
      }
    } catch (error){
      console.error(error);
      return false;
    }
  }
    public static async updateNotificationsEnabled(artistId: number, notificationsEnabled: number): Promise<boolean> {
      try {
        const response = await fetch(`/notification/UpdateNotificationsEnabled`, {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ artistId, notificationsEnabled }),
        });
        return response.ok;
      } catch (error) {
        console.error("Error updating notificationsEnabled:", error);
        return false;
      }
    }
  
}