using Microsoft.AspNetCore.Identity;

namespace ChatHubTest2.Models
{
    public class IdenUser : IdentityUser
    {
        public string? Login { get; set; }
    }
}
