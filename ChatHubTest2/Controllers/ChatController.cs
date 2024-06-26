﻿using ChatHubTest2.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

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
        private List<User> GetIdenUsers()
        {
            List<User> users = new List<User>();
            using (HttpClientHandler handler = new HttpClientHandler())
            {
                handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => { return true; };
                using (HttpClient client = new HttpClient(handler))
                {
                    string url = "https://chathubidentity:443/api/User";
                    HttpResponseMessage response = client.GetAsync(url).Result;
                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = response.Content.ReadAsStringAsync().Result;
                        List<User> userNames = JsonSerializer.Deserialize<List<User>>(responseBody);
                        users = userNames;
                    }
                }
            }
            return users;
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

            var user = GetIdenUsers().FirstOrDefault(x => x.Name == userNameModel.UserName);

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

            var chatCheck = new Chat();
            foreach (var chatId in chatIds)
            {
                var chatFound = _context.Chats.FirstOrDefault(x => x.Id.ToString() == chatId);
                if (chatFound?.Type == 1)
                {
                    chatCheck = chatFound;
                }
            }

            if (chatCheck.Type == 0)
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
            var user = GetIdenUsers().Where(x => x.Name == model.UserName).FirstOrDefault();
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
