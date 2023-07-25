using Microsoft.AspNetCore.SignalR;

namespace SimpleSignalrServer
{
    public class MyHub : Hub
    {
        public async Task SendMessage(string user, string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }

        public override async Task OnConnectedAsync()
        {
            Console.WriteLine($"ConnectionId {Context.ConnectionId} connected");
            await base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            Console.WriteLine($"ConnectionId {Context.ConnectionId} disconnected");
            return base.OnDisconnectedAsync(exception);
        }
    }
}
