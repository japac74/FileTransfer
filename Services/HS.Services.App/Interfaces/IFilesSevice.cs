using HS.Services.App.ModelsDto;

namespace HS.Services.App.Interfaces
{
    public interface IFilesService
    {
        Task<bool> CopyFile(FileCopyDto fileCopyDto);
    }
}
