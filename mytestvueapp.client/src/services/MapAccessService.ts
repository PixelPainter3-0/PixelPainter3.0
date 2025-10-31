import Point from "../entities/Point";
import Artspace from "../entities/Artspace";
import Art from "../entities/Art";


export default class MapAccessService {

    public static async getAllPoints(): Promise<Point[]> {
        try {
            const response = await fetch(
            `/mapaccess/GetAllPoints`
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

        public static async getPointById(pointId: number): Promise<Point> {
            try {
                const response = await fetch(
                    `mapaccess/GetPointById?id=${pointId}`
                );
                if (!response.ok) {
                    console.log("Bad response");
                    throw new Error("Error grabbing points");
                }
                console.log("Good response");
                console.log(response)
                const contentType = response.headers.get("content-type");
                if (!contentType || !contentType.includes("application/json")) {
                    const text = await response.text();
                    console.error("Unexpected content type. Response text:", text);
                    throw new Error("Expected JSON but got something else");
                }

                const json = await response.json();
                console.log(json)
                return json as Point;
            } catch (error) {
                console.error;
                throw error;
                //Unneeded?
                //return [];
            }
        }

    public static async getArtspacePoints(artspaceId: number): Promise<Point[]> {
        try {
            const response = await fetch(
                `/mapaccess/GetArtspacePoints?id=${artspaceId}`
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

    public static async getAllArtspaces(): Promise<Artspace[]> {
        try {
            const response = await fetch(
                `/mapaccess/GetAllArtspaces`
            );
            if (!response.ok) {
                console.log("Bad response");
                throw new Error("Error grabbing artspaces");
            }
            console.log("Good response");
            const json = await response.json();
            const artspaces: Artspace[] = [];

            for (const jsonArtspace of json) {
                artspaces.push(jsonArtspace as Artspace);
            }

            return artspaces;
        } catch (error) {
            console.error;
            throw error;
            //Unneeded?
            //return [];
        }
    }

    public static async getArtspaceById(artspaceId: number): Promise<Artspace> {
        try {
            const response = await fetch(
                `/mapaccess/GetArtspaceById?id=${artspaceId}`
            );
            if (!response.ok) {
                console.log("Bad response");
                throw new Error("Error grabbing artspaces");
            }
            console.log("Good response");
            const json = await response.json();
            const artspace = json as Artspace;

            return artspace;
        } catch (error) {
            console.error;
            throw error;
            //Unneeded?
            //return [];
        }
    }

    public static async getArtByPoint(pointId: number): Promise<Art[]> {
        try {
            const response = await fetch(
                `/mapaccess/GetArtByPoint?id=${pointId}`
            );
            if (!response.ok) {
                console.log("Bad response");
                throw new Error("Error grabbing points");
            }
            console.log("Good response");
            const json = await response.json();
            const pointArt: Art[] = [];

            for (const jsonPoint of json) {
                pointArt.push(jsonPoint as Art);
            }

            return pointArt;
        } catch (error) {
            console.error;
            throw error;
            //Unneeded?
            //return [];
        }
    }

    public static async createPoint(latitude: number, longitude: number, title: string, artspace: number): Promise<number | null > {
        try {
            const response = await fetch(`/mapaccess/CreatePoint?latitude=${latitude}&longitude=${longitude}&title=${title}&artspace=${artspace}`, {
                method: "PUT"
            });
            if (!response.ok) {
                throw new Error("Response was false.");
            }
            const data = await response.json();
            console.log("Raw response:", data);
            return data.pointId;
        } catch (error) {
            console.error(error);
            return null;
        }
    }

    public static async updateArtLocation(artId: number, pointId: number): Promise<boolean> {
        try {
            const response = await fetch(`/mapaccess/UpdateArtLocation?artId=${artId}&pointId=${pointId}`, {
                method: "PUT"
            });
            if (!response.ok) {
                throw new Error("Response was false.");
            }
            return true;
        } catch (error) {
            console.error(error);
            return false;
        }
    }
}
