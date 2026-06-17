using FileTransferTool.Validations;
using FluentValidation;
using HS.Services.App;
using HS.Services.App.Interfaces;
using HS.Services.App.ModelsDto;
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
services.AddSingleton<IValidator<FileCopyDto>>(new ValidateFilePaths());
services.AddSingleton<IFilesService, FilesService>();

// Build the provider
var provider = services.BuildServiceProvider();

// Run the app
var app = provider.GetRequiredService<Application>();
await app.Run();

Console.WriteLine("\n*** end of program ***");

// Proper cleanup
await provider.DisposeAsync();