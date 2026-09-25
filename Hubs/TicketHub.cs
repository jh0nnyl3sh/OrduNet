using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace OrduNet.Web.Hubs
{
    public class TicketHub : Hub
    {
        // Clients (Admin) will connect here to receive notifications.
        // We don't need any complex logic, just a simple broadcast.
    }
}
