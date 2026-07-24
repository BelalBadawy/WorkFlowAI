using Microsoft.AspNetCore.Hosting;
using WFAI.Application.Dtos.Common;
using WFAI.Application.Interfaces.Common;

namespace WFAI.Infrastructure.Services
{
    public class LocalFileStorageService : IFileStorageService
    {
        private readonly IWebHostEnvironment _webHostEnvironment;

        public LocalFileStorageService(IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<string> SaveFileAsync(FileData file, string folderName, CancellationToken ct)
        {
            if (file.Content.CanSeek)
            {
                file.Content.Position = 0;
            }

            if (string.IsNullOrEmpty(_webHostEnvironment.WebRootPath))
            {
                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", folderName);
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                var name = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                using (var stream = new FileStream(Path.Combine(path, name), FileMode.Create))
                {
                    await file.Content.CopyToAsync(stream, ct);
                }

                return name;
            }

            var uploadPath = Path.Combine(_webHostEnvironment.WebRootPath, folderName);

            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(uploadPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.Content.CopyToAsync(stream, ct);
            }

            return fileName;
        }

        public void DeleteFile(string fileName, string folderName)
        {
            var webRoot = _webHostEnvironment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var filePath = Path.Combine(webRoot, folderName, fileName);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }
}