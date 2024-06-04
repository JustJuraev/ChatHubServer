using ChatHubTest2.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ChatHubTest2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MessageController : ControllerBase
    {
        private ApplicationContext _context;

        public MessageController(ApplicationContext context)
        {
            _context = context;
        }

        [HttpPost]
        public IActionResult GetAllMessages([FromBody] ChatMembersUserId chatMembersUserId)
        {

            var test = chatMembersUserId;

            var chatIds = _context.ChatMembers
          .Where(cm => cm.UserId == chatMembersUserId.User1Id || cm.UserId == chatMembersUserId.User2Id)
          .GroupBy(cm => cm.ChatId)
          .Where(g => g.Count() == 2)
          .Select(g => g.Key)
          .ToList();

            

            var messages = new List<MessageUser>();
            if (chatIds.Count > 0)
            {
                var chatMessages = _context.Messages.Where(x => x.ChatId == chatIds.First()).ToList();
                foreach (var chatMessage in chatMessages)
                {
                    ////Это по сути изменяет статус новых сообщений.
                    //if (chatMessage.SenderId == chatMembersUserId.User2Id && chatMessage.StatusOfRead == 200)
                    //{
                    //    chatMessage.StatusOfRead = 300;
                    //    _context.Messages.Update(chatMessage);
                    //    _context.SaveChanges();
                    //}

                    var user = _context.Users.FirstOrDefault(x => x.Id.ToString() == chatMessage.SenderId);
                    if (user != null)
                    {
                        var messageUser = new MessageUser
                        {
                           Id = chatMessage.Id,
                           //ChatId = chatMessage.ChatId,
                           TempId = chatMessage.TempId,
                         //  RecipientId = chatMessage.RecipientId,
                         //  SenderId = chatMessage.SenderId,
                           SendTime = chatMessage.SendTime,
                           StatusSender = chatMessage.SenderStatus,
                           StatusRecipient = chatMessage.StatusRecipient,
                           StringText = chatMessage.StringText,
                           UserName = user.Name
                        };
                        messages.Add(messageUser);
                    }
                }
            }

            return Ok(messages);
        }
    }
}
