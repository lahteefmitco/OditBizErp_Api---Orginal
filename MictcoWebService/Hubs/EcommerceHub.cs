using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace MictcoWebService.Hubs
{
    public class EcommerceHub: Hub
    {
        // Optional: send message to specific user/group if needed
        public async Task SendMessage(string user, string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }
    }
}
