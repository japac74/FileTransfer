using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

// Set appsettings.json
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

// Set DI container
ServiceCollection services = new ServiceCollection();

// Configure and register Serilog
services.AddLogging(builder =>
{
    builder.AddSerilog(new LoggerConfiguration()
           .ReadFrom.Configuration(configuration)
           .CreateLogger()
    );
});

// Register main application class
services.AddSingleton<Application>();

// Add IConfiguration to DI
services.AddSingleton<IConfiguration>(configuration);

// Build the provider
var provider = services.BuildServiceProvider();

// Run the app
var app = provider.GetRequiredService<Application>();
app.Run();

// Proper cleanup
await provider.DisposeAsync();