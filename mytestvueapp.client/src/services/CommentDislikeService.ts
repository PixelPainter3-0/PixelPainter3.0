export default class CommentDislikeService {
    public static async insertCommentDislike(commentId: number): Promise<boolean> {
        try {
            const response = await fetch(`/commentdislike/InsertCommentDislike?commentId=${commentId}`, {
                method: "POST",
                headers: { "Content-Type": "application/json" }
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
    public static async removeCommentDislike(commentId: number): Promise<boolean> {
        try {
            const response = await fetch(`/commentdislike/RemoveCommentDislike?commentId=${commentId}`, {
                method: "DELETE",
                headers: { "Content-Type": "application/json" }
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
    public static async isCommentDisliked(commentId: number): Promise<boolean> {
        try {
            const response = await fetch(`/commentdislike/IsCommentDisliked?commentId=${commentId}`);
            if (!response.ok) {
                throw new Error("Response was false.");
            }
            const isCommentDisliked: boolean = (await response.json()) as boolean;
            return isCommentDisliked;
        } catch (error) {
            console.error(error);
            return false;
        }
    }
}
