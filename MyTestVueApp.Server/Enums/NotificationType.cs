namespace MyTestVueApp.Server.Enums
{
    public enum NotificationType
    {
        None = 0,
        ArtLiked = 1,           
        ArtCommented = 2,       
        CommentReplied = 4,     
        CommentLiked = 8,       
        CommentDisliked = 16,   
        ArtDisliked = 32        

        //All Enabled = 63
    }
}
