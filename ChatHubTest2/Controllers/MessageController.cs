﻿using ChatHubTest2.Models;
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

        [HttpPost("getMessagesfromGroupChat")]
        public IActionResult GetAllMessagesFromGroupChat([FromBody] UserGroup userGroup)
        {
            var messages = _context.Messages.Where(x => x.ChatId == userGroup.ChatId).ToList();
            List<MessageUser> messageUsers = new List<MessageUser>();

            foreach (var message in messages)
            {
                var user = _context.Users.FirstOrDefault(x => x.Id.ToString() == message.SenderId);
                var userRecipient = _context.Users.FirstOrDefault(x => x.Id.ToString() == message.RecipientId);
                var messageUser = new MessageUser
                {
                    Id = message.Id,
                    SendTime = message.SendTime,
                    StatusRecipient = message.StatusRecipient,
                    StatusSender = message.SenderStatus,
                    StringText = message.StringText,
                    TempId = message.TempId,
                    UserSenderName = user.Name,
                    UserRecipientName = userGroup.ChatName,
                    GroupId = message.ChatId,
                    IsDeleted = message.IsDeleted,
                    IsUpdated = message.IsUpdated,
                };
                messageUsers.Add(messageUser);
            }

            return Ok(messageUsers);
        }


        [HttpPost("getMesagesfromUserChat")]
        public IActionResult GetAllMessagesFromUserChat([FromBody] ChatMembersUserId chatMembersUserId)
        {

           

            var chatIds = _context.ChatMembers
          .Where(cm => cm.UserId == chatMembersUserId.User1Id || cm.UserId == chatMembersUserId.User2Id)
          .GroupBy(cm => cm.ChatId)
          .Where(g => g.Count() == 2)
          .Select(g => g.Key)
          .ToList();



            var messages = new List<MessageUser>();
            if (chatIds.Count > 0)
            {
                var chat = new Chat();
                foreach (var item in chatIds)
                {
                    var ch = _context.Chats.FirstOrDefault(x => x.Id.ToString() == item);
                    if (ch.Type == 1)
                        chat = ch;
                }
                var chatMessages = _context.Messages.Where(x => x.ChatId == chat.Id.ToString()).ToList();
                foreach (var chatMessage in chatMessages)
                {

                    var user = _context.IdenUsers.FirstOrDefault(x => x.Id == chatMessage.SenderId);
                    var userRecipient = _context.IdenUsers.FirstOrDefault(x => x.Id == chatMessage.RecipientId);
                    if (user != null)
                    {
                        var messageUser = new MessageUser
                        {
                            Id = chatMessage.Id,
                            TempId = chatMessage.TempId,
                            SendTime = chatMessage.SendTime,
                            StatusSender = chatMessage.SenderStatus,
                            StatusRecipient = chatMessage.StatusRecipient,
                            StringText = chatMessage.StringText,
                            UserSenderName = user.UserName,
                            UserRecipientName = userRecipient.UserName,
                            IsDeleted = chatMessage.IsDeleted,
                            IsUpdated = chatMessage.IsUpdated,
                        };
                        messages.Add(messageUser);
                    }
                }
            }

            return Ok(messages);
        }
    }
}
