using Microsoft.AspNetCore.SignalR;
using Npgsql.Replication.PgOutput.Messages;

namespace ChatHubTest2.Models
{

    public class ChatHub : Hub
    {
        private static Dictionary<string, string> ClientConnections = new Dictionary<string, string>();
        private ApplicationContext _context;

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

            if (chatIds.Count > 0)
                return chatIds.First();

            return "";
        }

        public async Task SendToOnePerson(string recipient, MessageUser message)
        {
            
            var recipientId = _context.Users.FirstOrDefault(x => x.Name == recipient);
            var cc = ClientConnections.Where(x => x.Value == Context.ConnectionId).FirstOrDefault();
            var senderId = _context.Users.Where(x => x.Name == cc.Key).FirstOrDefault();
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
                   
                    var client = Clients.Client(ClientConnections[recipient]);
                    msgU.UserName = senderId.Name;
                    msgU.StatusSender = 300;
                    await Clients.Client(Context.ConnectionId).SendAsync("PrivateMessage", "ds", msgU);
                    string updatedMessage = $"{senderId.Name}:{message}     {msg.SendTime}";
                    await client.SendAsync("PrivateMessage", updatedMessage, msgU);
                    
                }
                catch (Exception ex)
                {
                    await Clients.Caller.SendAsync("PrivateMessage", ex.Message);
                }
            }
            else
            {
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
            if (messages.Count > 0 && ClientConnections[clientName.UserName] != null)
            {
                var client = Clients.Client(ClientConnections[clientName.UserName]);
                await client.SendAsync("ReadMessages", messages);
            }
        }

       
        public override async Task OnConnectedAsync()
        {
            var clientId = Context.GetHttpContext().Request.Query["clientId"];
            ClientConnections[clientId] = Context.ConnectionId;
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
            var users = _context.Users.ToList();
            foreach (var user in users)
            {
                var userOnline = new UserOnlineStatus
                {
                    IsOnline = false,
                    UserId = user.Id,
                    UserName = user.Name
                };

                if (ClientConnections.ContainsKey(user.Name))
                      userOnline.IsOnline = true;

                onlineUsers.Add(userOnline);
            }

            await Clients.All.SendAsync("OnlineUsers", onlineUsers);
        }

    }
}
