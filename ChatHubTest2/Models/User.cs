using System.Text.Json.Serialization;

namespace ChatHubTest2.Models
{
    public class User
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("chatID")]
        public int ChatID { get; set; }
    }
}
