namespace ChatHubTest2.Models
{
    public class UserOnlineStatus
    {
        public Guid UserId { get; set; }

        public string? UserName { get; set; }

        public bool? IsOnline { get; set; } 
    }
}
