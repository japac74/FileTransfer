using EnsureThat;
using FileTransferTool.Validations;
using FluentValidation;
using HS.Services.App.ModelsDto;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

public class Application
{
    private readonly ILogger<Application> _logger;
    private readonly IConfiguration _configuration;

    private FileCopyDto _fileCopyDto;
    private string _sourcePath;
    private string _targetPath;

    private readonly IValidator<FileCopyDto> _fileCopyValidator;
    

    public Application(ILogger<Application> logger, IConfiguration configuration, IValidator<FileCopyDto> fileCopyValidator)
    {
        _logger = EnsureArg.IsNotNull(logger, nameof(ILogger<Application>));
        _configuration = EnsureArg.IsNotNull(configuration, nameof(IConfiguration));

        _fileCopyValidator = EnsureArg.IsNotNull(fileCopyValidator, nameof(IValidator<FileCopyDto>));
    }


    public void Run()
    {
        try
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Copy a file from source to destination.  Please enter Full Path.");
            Console.WriteLine("e.g. c\\:source\\my_big_file.mp4");
            Console.WriteLine("================================================================\n");
            
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("Enter source path: ");
            _sourcePath = Console.ReadLine();

            Console.Write("Enter target path: ");
            _targetPath = Console.ReadLine();
            Console.Write("\n");

            //TODO - Remove hard coded paths and use the ones entered by the user
            _sourcePath = "C:\\SoftwareDevelopment\\HornetSecurity\\Source.mp4";
            _targetPath = "C:\\SoftwareDevelopment\\HornetSecurit\\target";

            // Prepare FileCopyDto object and validat paths and file name
            _fileCopyDto = new FileCopyDto()
            {
                FullSourcePath = _sourcePath,
                SourcePath = Path.GetDirectoryName(_sourcePath),
                FileName = Path.GetFileName(_sourcePath),
                TargetPath = _targetPath,
            };           
            
            var results = _fileCopyValidator.Validate(_fileCopyDto);

            if (!results.IsValid)
            {
                Console.ForegroundColor = ConsoleColor.Red;

                foreach (var failure in results.Errors)
                {
                    Console.WriteLine($"-> {failure.ErrorMessage}");
                }

                Console.ForegroundColor = ConsoleColor.White;
                return;
            }

            // Start copying the file            
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);

        }
        //if (allArgs.Length > 1)
        //{
        //    Console.WriteLine($"First real parameter: {allArgs[1]}");
        //}

    }
}