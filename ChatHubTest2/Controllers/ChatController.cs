using ChatHubTest2.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ChatHubTest2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private ApplicationContext _context;

        public ChatController(ApplicationContext context)
        {
            _context = context;
        }

       
        [HttpPost]
        public IActionResult Post([FromBody] ChatMembersUserId chatMembersUserId) 
        {
            var chatIds = _context.ChatMembers
           .Where(cm => cm.UserId == chatMembersUserId.User1Id || cm.UserId == chatMembersUserId.User2Id)
           .GroupBy(cm => cm.ChatId)
           .Where(g => g.Count() == 2)
           .Select(g => g.Key)
           .ToList();

            if (chatIds.Count() == 0)
            {
                var chat = new Chat
                {
                    Id = Guid.NewGuid(),
                    Created = DateTime.UtcNow,
                };
                _context.Chats.Add(chat);
                _context.SaveChanges();

                var chatmember1 = new ChatMember
                {
                    Id = Guid.NewGuid(),
                    UserId = chatMembersUserId.User1Id,
                    ChatId = chat.Id.ToString(),
                };

                var chatmember2 = new ChatMember
                {
                    Id = Guid.NewGuid(),
                    UserId = chatMembersUserId.User2Id,
                    ChatId = chat.Id.ToString(),
                };
                _context.ChatMembers.Add(chatmember1);
                _context.ChatMembers.Add(chatmember2);
                _context.SaveChanges();
            }

            return Ok(chatMembersUserId);
        }

    }
}
