using Microsoft.AspNetCore.Mvc;
using ToYouEMS.ToYouEMS.Core.Interfaces;

namespace ToYouEMS.ToYouEMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FileController : ControllerBase
    {
        private readonly IFileStorageService _fileStorageService;

        public FileController(IFileStorageService fileStorageService)
        {
            _fileStorageService = fileStorageService;
        }

        [HttpGet("{folder}/{fileName}")]
        public IActionResult DownloadFile(string folder, string fileName)
        {
            string fileUrl = _fileStorageService.GetFileUrl(fileName, folder);
            string filePath = _fileStorageService.GetFilePath(fileUrl);

            if (!_fileStorageService.FileExists(fileUrl))
            {
                return NotFound(new { message = "文件不存在" });
            }

            // 获取文件类型
            string contentType = GetContentType(filePath);

            // 获取原始文件名 (如果有存储)
            string originalFileName = fileName;

            // 读取文件内容并返回
            var fileBytes = System.IO.File.ReadAllBytes(filePath);
            return File(fileBytes, contentType, originalFileName);
        }

        [HttpGet("templates/{fileName}")]
        public IActionResult DownloadTemplate(string fileName)
        {
            string fileUrl = _fileStorageService.GetFileUrl(fileName, "templates");
            string filePath = _fileStorageService.GetFilePath(fileUrl);

            if (!_fileStorageService.FileExists(fileUrl))
            {
                return NotFound(new { message = "模板不存在" });
            }

            var fileBytes = System.IO.File.ReadAllBytes(filePath);
            return File(fileBytes, GetContentType(filePath), fileName);
        }

        private string GetContentType(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();

            return extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".txt" => "text/plain",
                _ => "application/octet-stream",
            };
        }
    }
}
