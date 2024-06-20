namespace ChatHubTest2.Models
{
    public class MessageUser
    {
        public Guid Id { get; set; }

        public Guid TempId { get; set; }

        public string? StringText { get; set; }

        public DateTime SendTime { get; set; }

        public int StatusSender { get; set; } //100 - Отправлено, 200-Доставлено, 300-Прочитано

        public int StatusRecipient { get; set; } //400 - новое

        public string? UserSenderName { get; set; }

        public string? UserRecipientName { get; set; }

        public string? GroupId { get; set; }

        public bool IsDeleted { get; set; }

        public bool IsUpdated { get; set; }

        public string? ParentMessageId { get; set; }
    }
}
