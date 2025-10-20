
export default class Point {
  //required
  id: number;
  latitude: number;
  longitude: number;
  artspaceId: number;
  title: string;
  isPublic: boolean;

  constructor() {
    this.id = 0;
    this.latitude = 38.897634;
    this.longitude = -77.036570;
    this.title = "";
    this.artspaceId = 0;
    this.isPublic = false;
  }
}
