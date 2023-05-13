using Microsoft.AspNetCore.SignalR;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI;
using Microsoft.SemanticKernel.Orchestration;

public class MessageHub : Hub
{
    private readonly ILogger<MessageHub> logger;
    private readonly IConfiguration config;
    private readonly IKernel kernel;

    public MessageHub(ILogger<MessageHub> logger, IConfiguration config, IKernel kernel)
    {
        this.logger = logger;
        this.config = config;
        this.kernel = kernel;
    }

    public override async Task OnConnectedAsync()
    {
        try
        {
            logger.LogInformation($"Connected. Connection ID: {Context.ConnectionId}");
            await base.OnConnectedAsync();
            await Clients.Client(Context.ConnectionId).SendAsync("Connected", new { ClientID = Context.ConnectionId });
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message);
        }
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        logger.LogInformation($"Disconnected. Connection ID: {Context.ConnectionId}");

        return base.OnDisconnectedAsync(exception);
    }
    public async Task Ask(string question, int seq)
    {
        try
        {
            var results = await kernel.Memory.SearchAsync(Context.ConnectionId, question, limit: 2).ToListAsync();
            var variables = new ContextVariables(question)
            {
                ["context"] = results.Any()
                    ? string.Join("\n", results.Select(r => r.Metadata.Text))
                    : "No context found for this question.",
                ["input"] = question,
            };

            var skill = kernel.Skills.GetFunction("chat", "answer");
            var result = await kernel.RunAsync(variables, skill);
            await Clients.Client(Context.ConnectionId).SendAsync("Reply", result.Result, seq);

        }
        catch (AIException ex)
        {
            logger.LogError(ex.Detail, ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message);
        }
    }

    public async Task Echo(string message)
    {
        await Clients.Client(Context.ConnectionId).SendAsync("Reply", message);
    }
}