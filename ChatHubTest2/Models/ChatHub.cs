﻿using Microsoft.AspNetCore.SignalR;

namespace ChatHubTest2.Models
{

    public class ChatHub : Hub
    {
        private static Dictionary<string, UserOnlineChat> ClientConnections = new Dictionary<string, UserOnlineChat>();
        private ApplicationContext _context;
        private static Dictionary<string, Dictionary<string, int>> newMessages = new Dictionary<string, Dictionary<string, int>>();

        public ChatHub(ApplicationContext context)
        {
            _context = context;
        }

        private string ReturnChatId(string user1Id, string user2Id)
        {
            var chatIds = _context.ChatMembers
           .Where(cm => cm.UserId == user1Id || cm.UserId == user2Id)
           .GroupBy(cm => cm.ChatId)
           .Where(g => g.Count() == 2)
           .Select(g => g.Key)
           .ToList();

            var finalChat = new Chat();
            foreach (var chatId in chatIds)
            {
                var chat = _context.Chats.FirstOrDefault(x => x.Id.ToString() == chatId);
                if (chat?.Type == 1)
                    finalChat = chat;
            }

            return finalChat.Id.ToString();
        }

        public async Task SendToOnePerson(string recipient, MessageUser message)
        {

            var recipientId = _context.IdenUsers.FirstOrDefault(x => x.UserName == recipient);
            var cc = ClientConnections.Where(x => x.Value.ConnectionId == Context.ConnectionId).FirstOrDefault();
            var senderId = _context.IdenUsers.Where(x => x.UserName == cc.Key).FirstOrDefault();
            var chatId = ReturnChatId(senderId.Id.ToString(), recipientId.Id.ToString());
            var msg = new Message
            {
                Id = Guid.NewGuid(),
                TempId = message.TempId,
                StringText = message.StringText,
                SendTime = DateTime.UtcNow,
                RecipientId = recipientId.Id.ToString(),
                SenderId = senderId.Id.ToString(),
                ChatId = chatId,
                SenderStatus = 200,
                StatusRecipient = 400
            };
            if (message.ParentMessageId != null)
            {
                msg.ParentMessageId = message.ParentMessageId.ToString();
            }

            _context.Messages.Add(msg);
            _context.SaveChanges();

            var msgU = new MessageUser
            {
                Id = msg.Id,
                TempId = msg.TempId,
                SendTime = msg.SendTime,
                StatusSender = msg.SenderStatus,
                StatusRecipient = msg.StatusRecipient,
                StringText = msg.StringText,

            };

            if (ClientConnections.ContainsKey(recipient))
            {
                try
                {
                    if (ClientConnections[recipient].Chat == senderId.UserName)
                    {
                        var client = Clients.Client(ClientConnections[recipient].ConnectionId);
                        msgU.UserSenderName = senderId.UserName;
                        msgU.UserRecipientName = recipientId.UserName;
                        msgU.StatusSender = 300;
                        msgU.StatusRecipient = 300;
                        await Clients.Client(Context.ConnectionId).SendAsync("PrivateMessage", "ds", msgU);
                        string updatedMessage = $"{senderId.UserName}:{message}     {msg.SendTime}";
                        await client.SendAsync("PrivateMessage", updatedMessage, msgU);
                    }
                    else
                    {
                        msgU.UserSenderName = senderId.UserName;
                        msgU.UserRecipientName = recipientId.UserName;
                        var client = Clients.Client(Context.ConnectionId);
                        await client.SendAsync("PrivateMessage", "", msgU);
                    }

                }
                catch (Exception ex)
                {
                    await Clients.Caller.SendAsync("PrivateMessage", ex.Message);
                }
            }
            else
            {
                msgU.UserSenderName = senderId.UserName;
                msgU.UserRecipientName = recipientId.UserName;
                var client = Clients.Client(Context.ConnectionId);
                await client.SendAsync("PrivateMessage", "", msgU);
            }
        }

        public async Task ConfirmReadLive(List<MessageUser> messages)
        {
            var clientName = messages.FirstOrDefault();

            foreach (var msg in messages)
            {
                var message = _context.Messages.FirstOrDefault(x => x.Id == msg.Id);
                msg.StatusSender = 300;
                message.SenderStatus = 300;
                message.StatusRecipient = 300;
                _context.Messages.Update(message);
                _context.SaveChanges();
            }
            if (messages.Count > 0 && ClientConnections[clientName.UserSenderName] != null)
            {
                var client = Clients.Client(ClientConnections[clientName.UserSenderName].ConnectionId);
                await client.SendAsync("ReadMessages", messages);
            }
        }

        public void UserCurrentChat(string user, string userChat)
        {
            var userConnection = ClientConnections[user];
            if (userConnection != null)
            {
                userConnection.Chat = userChat;
            }
            //var test = ClientConnections;
        }

        public override async Task OnConnectedAsync()
        {
            var clientId = Context.GetHttpContext().Request.Query["clientId"];
            ClientConnections.Add(clientId, new UserOnlineChat { ConnectionId = Context.ConnectionId, Chat = "" });
            //  ClientConnections[clientId].ConnectionId = Context.ConnectionId;

            await base.OnConnectedAsync();

        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var clientId = Context.GetHttpContext().Request.Query["clientId"];
            if (ClientConnections.ContainsKey(clientId))
            {
                ClientConnections.Remove(clientId);
            }
            await base.OnDisconnectedAsync(exception);
            ShowOnlineUsers();
        }

        public async Task ShowOnlineUsers()
        {
            List<UserOnlineStatus> onlineUsers = new List<UserOnlineStatus>();
            var users = _context.IdenUsers.ToList();
            foreach (var user in users)
            {
                var userOnline = new UserOnlineStatus
                {
                    IsOnline = false,
                    UserId = new Guid(user.Id),
                    UserName = user.UserName
                };

                if (ClientConnections.ContainsKey(user.UserName))
                    userOnline.IsOnline = true;

                onlineUsers.Add(userOnline);
            }

            await Clients.All.SendAsync("OnlineUsers", onlineUsers);
        }

        public async Task SendToGroup(MessageUser messageUser)
        {
            var cc = ClientConnections.Where(x => x.Value.ConnectionId == Context.ConnectionId).FirstOrDefault();
            var senderId = _context.IdenUsers.Where(x => x.UserName == cc.Key).FirstOrDefault();
            var msg = new Message
            {
                Id = Guid.NewGuid(),
                ChatId = messageUser.UserRecipientName,
                RecipientId = messageUser.UserRecipientName,
                SenderId = senderId.Id.ToString(),
                SenderStatus = 200,
                StatusRecipient = 400,
                SendTime = DateTime.UtcNow,
                StringText = messageUser.StringText,
                TempId = messageUser.TempId,
            };

            if (messageUser.ParentMessageId != null)
                msg.ParentMessageId = messageUser.ParentMessageId.ToString();

            _context.Messages.Add(msg);
            _context.SaveChanges();
            var groupName = _context.Chats.FirstOrDefault(x => x.Id.ToString() == msg.RecipientId);
            var msgU = new MessageUser
            {
                Id = msg.Id,
                TempId = msg.TempId,
                SendTime = msg.SendTime,
                StatusSender = msg.SenderStatus,
                StatusRecipient = msg.StatusRecipient,
                StringText = msg.StringText,
                UserRecipientName = groupName.ChatName,
                UserSenderName = senderId.UserName,
                GroupId = msg.ChatId

            };

            var usersId = _context.ChatMembers.Where(x => x.ChatId == msg.ChatId && x.UserId != msg.SenderId).Select(x => x.UserId).ToList();
            foreach (var user in usersId)
            {
                var userName = _context.Users.FirstOrDefault(x => x.Id.ToString() == user);
                if (ClientConnections.ContainsKey(userName.Name) && ClientConnections[userName.Name].Chat == msg.ChatId)
                {
                    //var clientOnline = Clients.Client(ClientConnections[userName.Name].ConnectionId);
                    var test = ClientConnections[userName.Name];
                    msgU.StatusSender = 300;
                    msgU.StatusRecipient = 300;

                    await Clients.Client(ClientConnections[userName.Name].ConnectionId).SendAsync("GroupMessage", msgU);
                    await Clients.Client(Context.ConnectionId).SendAsync("GroupMessage", msgU);

                    //   break;
                }
            }
            if (msgU.StatusSender != 300)
            {
                var client = Clients.Client(Context.ConnectionId);
                await client.SendAsync("GroupMessage", msgU);
            }
        }

        public void NewMessagesRead(string recipient)
        {
            var cc = ClientConnections.Where(x => x.Value.ConnectionId == Context.ConnectionId).FirstOrDefault();
            var senderId = _context.IdenUsers.Where(x => x.UserName == cc.Key).FirstOrDefault();
            var test = newMessages;
            if (newMessages[senderId.UserName].ContainsKey(recipient))
            {
                newMessages[senderId.UserName].Remove(recipient);

            }

            Clients.Client(Context.ConnectionId).SendAsync("ReadMessagesLive", newMessages[senderId.UserName]);
        }

        public void NewMessagesLive(string recipient, MessageUser message)
        {

            var user = _context.IdenUsers.FirstOrDefault(x => x.UserName == recipient);
            var chat = _context.Chats.FirstOrDefault(x => x.Id.ToString() == recipient);
            var cc = ClientConnections.Where(x => x.Value.ConnectionId == Context.ConnectionId).FirstOrDefault();
            var senderId = _context.IdenUsers.Where(x => x.UserName == cc.Key).FirstOrDefault();
            if (user != null)
            {
                if (ClientConnections.ContainsKey(recipient))
                {
                    if (ClientConnections[recipient].Chat != senderId.UserName)
                    {
                        // var test2 = newMessages;
                        if (!newMessages[recipient].ContainsKey(senderId.UserName))
                        {
                            newMessages[recipient].Add(senderId.UserName, 1);
                        }
                        else
                        {
                            newMessages[recipient][senderId.UserName] += 1;
                        }
                    }
                }
                if (ClientConnections.ContainsKey(recipient))
                {

                    Clients.Client(ClientConnections[recipient].ConnectionId).SendAsync("ReadMessagesLive", newMessages[recipient]);
                }
            }

            if (chat != null)
            {
                var usersId = _context.ChatMembers.Where(x => x.ChatId == recipient && x.UserId != senderId.Id.ToString()).Select(x => x.UserId).ToList();
                User userSend = new User();
                foreach (var item in usersId)
                {
                    var userName = _context.Users.FirstOrDefault(x => x.Id.ToString() == item);

                    if (ClientConnections.ContainsKey(userName.Name) && ClientConnections[userName.Name].Chat != recipient)
                    {
                        userSend = userName;
                        if (!newMessages[userSend.Name].ContainsKey(recipient))
                        {
                            newMessages[userSend.Name].Add(recipient, 1);
                        }
                        else
                        {
                            newMessages[userSend.Name][recipient] += 1;
                        }
                        if (userSend.Name != null && ClientConnections.ContainsKey(userSend.Name))
                        {

                            Clients.Client(ClientConnections[userSend.Name].ConnectionId).SendAsync("ReadMessagesLive", newMessages[userSend.Name]);
                        }
                        //break;
                    }
                }
                
            }

        }

        public void NewMessages(string currentUserName)
        {
            if (!newMessages.ContainsKey(currentUserName))
            {
                newMessages.Add(currentUserName, new Dictionary<string, int>());
            }
            var currentUser = _context.IdenUsers.FirstOrDefault(x => x.UserName == currentUserName);
            var users = _context.IdenUsers.Where(x => x.Id != currentUser.Id).ToList();
            foreach (var user in users)
            {
                var messages = _context.Messages.Where(x => x.SenderId == user.Id.ToString() && x.RecipientId == currentUser.Id.ToString() && x.StatusRecipient == 400 && x.IsDeleted == false).ToList();
                if (!newMessages[currentUserName].ContainsKey(user.UserName))
                {
                    newMessages[currentUserName].Add(user.UserName, messages.Count());
                }
                newMessages[currentUserName][user.UserName] = messages.Count();
            }
            var chatMembers = _context.ChatMembers.Where(x => x.UserId == currentUser.Id.ToString()).ToList();
            foreach (var chatMember in chatMembers)
            {
                var chat = _context.Chats.FirstOrDefault(x => x.Id.ToString() == chatMember.ChatId && x.Type == 2);
                if (chat != null)
                {
                    var chusers = _context.ChatMembers.Where(x => x.UserId != currentUser.Id.ToString() && x.ChatId == chat.Id.ToString()).ToList();
                    foreach (var item in chusers)
                    {
                        var messages = _context.Messages.Where(x => x.SenderId == item.UserId && x.RecipientId == chat.Id.ToString() && x.StatusRecipient == 400 && x.IsDeleted == false).ToList();
                        if (!newMessages[currentUserName].ContainsKey(chat.Id.ToString()))
                        {
                            newMessages[currentUserName].Add(chat.Id.ToString(), messages.Count());
                        }
                        newMessages[currentUserName][chat.Id.ToString()] += messages.Count();
                    }

                }
            }


            Clients.Client(Context.ConnectionId).SendAsync("ReadMessagesLive", newMessages[currentUserName]);
        }

        public void MessageStatusToUpdate(int index, MessageUser message, string recipient)
        {
            var msg = _context.Messages.FirstOrDefault(x => x.Id == message.Id);
            var cc = ClientConnections.Where(x => x.Value.ConnectionId == Context.ConnectionId).FirstOrDefault();
            var senderId = _context.Users.Where(x => x.Name == cc.Key).FirstOrDefault();
            if (msg != null)
            {
                msg.IsUpdated = true;
                msg.StringText = message.StringText;
                _context.Messages.Update(msg);
                _context.SaveChanges();

                Clients.Client(Context.ConnectionId).SendAsync("MessageUpdate", index, message);
                if (ClientConnections.ContainsKey(recipient) && ClientConnections[recipient].Chat == senderId.Name)
                {
                    Clients.Client(ClientConnections[recipient].ConnectionId).SendAsync("MessageUpdate", index, message);
                }
                var chat = _context.Chats.FirstOrDefault(x => x.Id.ToString() == recipient);
                if (chat != null)
                {
                    var chatMembers = _context.ChatMembers.Where(x => x.ChatId == chat.Id.ToString() && x.UserId != senderId.Id.ToString()).Select(x => x.UserId).ToList();
                    foreach (var chatMember in chatMembers)
                    {
                        var user = _context.Users.FirstOrDefault(x => x.Id.ToString() == chatMember);
                        if (ClientConnections.ContainsKey(user.Name) && ClientConnections[user.Name].Chat == chat.Id.ToString())
                        {
                            Clients.Client(ClientConnections[user.Name].ConnectionId).SendAsync("MessageUpdate", index, message);
                        }
                    }
                }
            }
        }

        //public void MessageAnswer(MessageUser message)
        //{
        //    var msg = _context.Messages.FirstOrDefault(x => x.Id == message.Id);
        //    if (msg != null)
        //    {
        //    }
        //}

        public void MessageStatusToDelete(int index, MessageUser message, string recipient)
        {
            var msg = _context.Messages.FirstOrDefault(x => x.Id == message.Id);
            var cc = ClientConnections.Where(x => x.Value.ConnectionId == Context.ConnectionId).FirstOrDefault();
            var senderId = _context.Users.Where(x => x.Name == cc.Key).FirstOrDefault();
            if (msg != null)
            {
                msg.IsDeleted = true;
                _context.Messages.Update(msg);
                _context.SaveChanges();

                Clients.Client(Context.ConnectionId).SendAsync("MessageDelete", index);
                if (ClientConnections.ContainsKey(recipient) && ClientConnections[recipient].Chat == senderId.Name)
                {
                    Clients.Client(ClientConnections[recipient].ConnectionId).SendAsync("MessageDelete", index);
                }
                var chat = _context.Chats.FirstOrDefault(x => x.Id.ToString() == recipient);
                if (chat != null)
                {
                    var chatMembers = _context.ChatMembers.Where(x => x.ChatId == chat.Id.ToString() && x.UserId != senderId.Id.ToString()).Select(x => x.UserId).ToList();
                    foreach (var chatMember in chatMembers)
                    {
                        var user = _context.Users.FirstOrDefault(x => x.Id.ToString() == chatMember);
                        if (ClientConnections.ContainsKey(user.Name) && ClientConnections[user.Name].Chat == chat.Id.ToString())
                        {
                            Clients.Client(ClientConnections[user.Name].ConnectionId).SendAsync("MessageDelete", index);
                        }
                    }
                }
            }
        }
    }
}
