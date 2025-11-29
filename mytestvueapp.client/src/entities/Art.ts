import { PixelGrid } from "./PixelGrid";
import type Tag from "./Tag";

export default class Art {
  //required
  id: number;
  artistId: number[];
  title: string;
  isPublic: boolean;
  creationDate: string;

  tags: Tag[];

  //may be null if new
  pixelGrid: PixelGrid;

  //optional
  artistName: string[];
  numLikes: number;
  numDislikes: number;
  numComments: number;
  currentUserIsOwner: boolean;
  isLiked: boolean;
  isDisliked: boolean;

  isGif: boolean;
  gifID: number;
  gifFrameNum: number;
  gifFps: number;

  pointId: number;
  pointTitle: string;
  artspaceId: number;
  artspaceTitle: string;


  constructor() {
    this.id = 0;
    this.title = "";
    this.artistId = [];
    this.artistName = [];

    this.creationDate = "";
    this.isPublic = false;
    this.numLikes = 0;
    this.numDislikes = 0;
    this.numComments = 0;
    this.pixelGrid = new PixelGrid(1, 1, "FF0000", false);
    this.currentUserIsOwner = false;

    this.isGif = false;
    this.gifFrameNum = 0;
    this.gifID = 0;
    this.gifFps = 0;
    this.isLiked = false;

    this.tags = []; // Initialize Tags as an empty array
    this.isDisliked = false;

    this.pointId = 0;
    this.pointTitle = "";
    this.artspaceId = 0;
    this.artspaceTitle = "";
  }
}
