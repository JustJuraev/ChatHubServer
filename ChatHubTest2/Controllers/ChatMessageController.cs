using ChatHubTest2.Models;
using Microsoft.AspNetCore.Mvc;

namespace ChatHubTest2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatMessageController : ControllerBase
    {
        private ApplicationContext _context;

        public ChatMessageController(ApplicationContext context)
        {
            _context = context;
        }

        [HttpPost]
        public IActionResult AddChatMember([FromBody] UserGroup userGroup)
        {
            var chatMembers = _context.ChatMembers.Where(x => x.ChatId == userGroup.ChatId).ToList();
            var user = _context.Users.FirstOrDefault(x => x.Name == userGroup.ChatMembers);
            var userContains = chatMembers.FirstOrDefault(x => x.UserId == user.Id.ToString());
            if (userContains != null)
            {
                return BadRequest("Пользователь уже есть в группе");
            }
            var chatMember = new ChatMember
            {
                Id = Guid.NewGuid(),
                ChatId = userGroup.ChatId,
                UserId = user.Id.ToString(),
            };
            _context.ChatMembers.Add(chatMember);
            _context.SaveChanges();
            return Ok("Успешно добавлено");
        }
    }
}
