namespace ChatHubTest2.Models
{
    public class Message
    {
        public Guid Id { get; set; }

        public Guid TempId { get; set; }

        public string? StringText { get; set; }

        public DateTime SendTime { get; set; }

        public string? RecipientId { get; set; }

        public string? SenderId { get; set; }

        public string? ChatId { get; set; }

        public int SenderStatus { get; set; } //100 - Отправлено, 200-Доставлено, 300-Прочитано

        public int StatusRecipient { get; set; } //400 - новое

        public bool IsDeleted { get; set; }

        public bool IsUpdated { get; set; }

        public string? ParentMessageId { get; set; }
    }
}
