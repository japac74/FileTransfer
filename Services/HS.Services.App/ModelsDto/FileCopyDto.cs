namespace HS.Services.App.ModelsDto
{
    public class FileCopyDto
    {
        public required string FileName { get; set; }

        public required string SourcePath { get; set; }
               
        public required string TargetPath { get; set; }

        public required string FullSourcePath { get; set; }
    }
}
