using FluentValidation;
using HS.Services.App.ModelsDto;

namespace FileTransferTool.Validations
{
    public class ValidateFilePaths : AbstractValidator<FileCopyDto>
    {
        public ValidateFilePaths() 
        {
            RuleFor(x => x.FileName)
                .NotEmpty().WithMessage("File name is required.");

            RuleFor(x => x.FullSourcePath)
                .Must(PathExists).WithMessage("Source path or the specified file does not exist.");

            RuleFor(x => x.SourcePath)
                .NotEmpty().WithMessage("Source path is required.")
                .Must(DirectoryExists).WithMessage("The source directory does not exist.");

            // 1. Validate a File Path
            RuleFor(x => x.TargetPath)
                .NotEmpty().WithMessage("Target path is required.")
                .Must(DirectoryExists).WithMessage("The target directory does not exist.");
        }



        /// <summary>
        /// Helper method to check if a file exists at the given path.
        /// </summary>
        /// <param name="path">Path to check.</param>
        /// <returns>true if the path exists; otherwise, false.</returns>
        private bool PathExists(string path)
        {
            return File.Exists(path);
        }

        /// <summary>
        /// Helper method to check if a directory exists at the given path.
        /// </summary>
        /// <param name="path">Directory path to check.</param>
        /// <returns>true if the directory exists; otherwise, false.</returns>
        private bool DirectoryExists(string path)
        {
            return Directory.Exists(path);        
        }
    }
}
