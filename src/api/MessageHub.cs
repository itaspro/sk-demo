using app.Controllers;
using Microsoft.AspNetCore.SignalR;
using Microsoft.SemanticKernel;

public class MessageHub : Hub
{
    private readonly ILogger<FileUploadController> logger;
    private readonly IConfiguration config;
    private readonly IKernel kernel;

    public MessageHub(ILogger<FileUploadController> logger, IConfiguration config, IKernel kernel)
    {
        this.logger = logger;
        this.config = config;
        this.kernel = kernel;
    }

    public void Ask(string message)
    {
        Clients.Client(Context.ConnectionId).SendAsync("Reply", message + " (echo from server)");
    }
}