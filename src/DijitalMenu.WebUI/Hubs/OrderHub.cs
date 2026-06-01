using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace DijitalMenu.WebUI.Hubs;

[Authorize(Roles = "Admin,Garson,Mutfak")]
public sealed class OrderHub : Hub;
