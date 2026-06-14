using EnsureThat;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

public class Application
{
    private readonly ILogger<Application> _logger;
    private readonly IConfiguration _configuration;

    public Application(ILogger<Application> logger, IConfiguration configuration)
    {
        _logger = EnsureArg.IsNotNull(logger, nameof(ILogger<Application>));
        _configuration = EnsureArg.IsNotNull(configuration, nameof(IConfiguration));
    }

    public void Run()
    {
        _logger.LogWarning("Hello, World!");
        
    }
}