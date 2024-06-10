namespace ChatHubTest2.Models
{
    public class Chat
    {
        public Guid Id { get; set; }

        public DateTime Created { get; set; }

        public string ChatName { get; set; }

        public int Type { get; set; }
    }
}
