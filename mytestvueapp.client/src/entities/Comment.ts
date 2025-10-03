export default class Comment {
  id?: number;
  artistId?: number;
  artId?: number;
  message?: string;
  commenterName?: string;
  creationDate: string;
  replyId?: number;
  currentUserIsOwner?: boolean;
  replies: Comment[];
  isViewed: boolean;
  numLikes: number = 0;
  numDislikes: number = 0;
  isLiked: boolean;
  isDisliked: boolean;

  constructor() {
    this.id = 0;
    this.artistId = 0;
    this.artId = 0;
    this.message = "";
    this.commenterName = "unknown";
    this.creationDate = "";
    this.replyId = 0;
    this.currentUserIsOwner = false;
    this.replies=[];
    this.isViewed=false;
    this.numLikes = 0;
    this.numDislikes = 0;
    this.isLiked = false;
    this.isDisliked =false;
  }
}
