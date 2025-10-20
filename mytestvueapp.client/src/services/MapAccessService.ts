import Point from "../entities/Point";

export default class MapAccessService {

  public static async getArtspacePoints(ArtspaceId: number): Promise<Point[]> {
    try {
      const response = await fetch(
        `/mapaccess/GetArtspacePoints?ArtspaceId=${ArtspaceId}`
      );
      if (!response.ok) {
        console.log("Bad response");
        throw new Error("Error grabbing points");
      }
      console.log("Good response");
      const json = await response.json();
      const artspacePoints: Point[] = [];

      for (const jsonPoint of json) {
        artspacePoints.push(jsonPoint as Point);
      }

      return artspacePoints;
    } catch (error) {
      console.error;
      throw error;
      //Unneeded?
      //return [];
    }
  }

}
