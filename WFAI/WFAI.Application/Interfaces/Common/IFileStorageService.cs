using WFAI.Application.Dtos.Common;

namespace WFAI.Application.Interfaces.Common
{
    public interface IFileStorageService
    {
        Task<string> SaveFileAsync(FileData file, string folderName, CancellationToken ct = default);

        void DeleteFile(string fileName, string folderName);
    }
}