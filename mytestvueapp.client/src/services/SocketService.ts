import GroupAdvert from "@/entities/GroupAdvert";
export default class SocketService {
  public static async getAllGroups(): Promise<GroupAdvert[]> {
    try {
      const response = await fetch("/socket/GetGroups");
      const json = await response.json();

      const allGroups: GroupAdvert[] = [];

      for (const jsonGroup of json) {
        let group = new GroupAdvert();
        group = jsonGroup as GroupAdvert;
        allGroups.push(group);
      }
  
      return allGroups;
    } catch (error) {
      console.error;
      throw error;
    }
  }
  public static async DisableGrid(): Promise<void> {
    const response = await fetch("/socket/DisableGrid", { method: "POST" });
    const json = await response.json();
    if (!json.success) {
      throw new Error("Failed to disable grid");
    }
  }
  public static async EnableGrid(): Promise<void> {
    const response = await fetch("/socket/EnableGrid", { method: "POST" });
    const json = await response.json();
    if (!json.success) {
      throw new Error("Failed to enable grid");
    }
  }
  public static async SaveGrid(name: string): Promise<void> {
    const request = '/socket/SaveGrid';
    const response = await fetch(request, {
        method: "POST",
        body: JSON.stringify(name),
        headers: { "Content-Type": "application/json" }
      });
    // const response = await fetch("/socket/SaveGrid", { 
    //   method: "POST",
    //   headers: {
    //     "Content-Type": "application/json"
    //   },
    //   body: JSON.stringify({ name })
    // });
    const json = await response.json();
    if (!json){
      throw new Error("Failed to save grid");
    }
  }
}
