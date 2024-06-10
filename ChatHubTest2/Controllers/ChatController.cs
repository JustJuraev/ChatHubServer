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


        [HttpPost("getUsersGroup")]
        public IActionResult GetUsersGroups(UserNameModel userNameModel)
        {
            var groups = from chm in _context.ChatMembers
                         join ch in _context.Chats on chm.ChatId equals ch.Id.ToString()
                         select new UserGroup
                         {
                             ChatId = ch.Id.ToString(),
                             ChatName = ch.ChatName,
                             ChatMembers = chm.UserId
                         };

            var user = _context.Users.FirstOrDefault(x => x.Name == userNameModel.UserName);

            var finalGroups = groups.Where(x => x.ChatMembers == user.Id.ToString() && x.ChatName != "").ToList();
            return Ok(finalGroups);
        }


        [HttpPost("createChat")]
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
                    ChatName = "",
                    Type = 1
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

        [HttpPost("createChatGroup")]
        public IActionResult CreateChatGroup([FromBody] GroupNameModel model)
        {
            var chat = new Chat
            {
                Id = Guid.NewGuid(),
                Created = DateTime.UtcNow,
                ChatName = model.GroupName,
                Type = 2
            };
            var user = _context.Users.Where(x => x.Name == model.UserName).FirstOrDefault();
            var chatmember = new ChatMember
            {
                Id = Guid.NewGuid(),
                UserId = user.Id.ToString(),
                ChatId = chat.Id.ToString(),
            };

            _context.Chats.Add(chat);
            _context.ChatMembers.Add(chatmember);
            _context.SaveChanges();

            return Ok("200");
        }

    }
}
