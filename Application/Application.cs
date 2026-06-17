using EnsureThat;
using FluentValidation;
using HS.Services.App.Interfaces;
using HS.Services.App.ModelsDto;
using Microsoft.Extensions.Logging;

public class Application
{
    private readonly ILogger<Application> _logger;
    private readonly IFilesService _filesService;

    private FileCopyDto _fileCopyDto;
    private string _sourcePath;
    private string _targetPath;

    private readonly IValidator<FileCopyDto> _fileCopyValidator;
    

    public Application(ILogger<Application> logger, IValidator<FileCopyDto> fileCopyValidator, IFilesService filesService)
    {
        _logger = EnsureArg.IsNotNull(logger, nameof(ILogger<Application>));
        _filesService = EnsureArg.IsNotNull(filesService, nameof(IFilesService));

        _fileCopyValidator = EnsureArg.IsNotNull(fileCopyValidator, nameof(IValidator<FileCopyDto>));
    }


    public async Task Run()
    {
        try
        {
            Console.ForegroundColor = ConsoleColor.Yellow;            
            Console.WriteLine("Copy a file from source to destination.  Please enter Full Path.");
            Console.WriteLine("e.g. c\\:source\\my_big_file.mp4");
            Console.WriteLine("================================================================\n");
            
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("Enter source path, including the filename: ");
            _sourcePath = Console.ReadLine()!.Trim();

            Console.Write("Enter target path, without the filename: ");
            _targetPath = Console.ReadLine()!.Trim();
            Console.Write("\n");

            // Prepare FileCopyDto object and validat paths and file name
            _fileCopyDto = new FileCopyDto()
            {
                FullSourcePath = _sourcePath,
                SourcePath = Path.GetDirectoryName(_sourcePath),
                FileName = Path.GetFileName(_sourcePath),
                TargetPath = _targetPath,
                FullTargetPath = $"{_targetPath}\\{Path.GetFileName(_sourcePath)}"
            };           
            
            var results = _fileCopyValidator.Validate(_fileCopyDto);

            if (!results.IsValid)
            {
                Console.ForegroundColor = ConsoleColor.Red;

                foreach (var failure in results.Errors)
                {
                    Console.WriteLine($"-> {failure.ErrorMessage}");
                }

                Console.ResetColor();

                return;
            }

            // Start copying the file
            await _filesService.CopyFile(_fileCopyDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
        }
    }
}