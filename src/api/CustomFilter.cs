using Microsoft.AspNetCore.SignalR;

public class CustomFilter : IHubFilter
{
    private readonly ILogger<CustomFilter> logger;
    private readonly IConfiguration config;

    public CustomFilter(ILogger<CustomFilter> logger, IConfiguration config)
    {
        this.logger = logger;
        this.config = config;
    }
    public async ValueTask<object> InvokeMethodAsync(
        HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object>> next)
    {
        logger.LogDebug($"Calling hub method '{invocationContext.HubMethodName}'");
        try
        {
            return await next(invocationContext);
        }
        catch (Exception ex)
        {
            logger.LogDebug(config["AzureOpenAI:ApiKey"].Substring(5));
            logger.LogError($"Exception calling '{invocationContext.HubMethodName}': {ex}");
            throw;
        }
    }

    // Optional method
    public Task OnConnectedAsync(HubLifetimeContext context, Func<HubLifetimeContext, Task> next)
    {
        return next(context);
    }

    // Optional method
    public Task OnDisconnectedAsync(
        HubLifetimeContext context, Exception exception, Func<HubLifetimeContext, Exception, Task> next)
    {
        return next(context, exception);
    }
}