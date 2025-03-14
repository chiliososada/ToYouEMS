using ToYouEMS.ToYouEMS.Core.Interfaces;

namespace ToYouEMS.ToYouEMS.Infrastructure.Services
{
    public class LocalFileStorageService : IFileStorageService
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IConfiguration _configuration;
        private readonly string _baseStoragePath;
        private readonly string _baseUrl;

        public LocalFileStorageService(IWebHostEnvironment webHostEnvironment, IConfiguration configuration)
        {
            _webHostEnvironment = webHostEnvironment;
            _configuration = configuration;

            // 配置文件存储的根目录和访问URL
            _baseStoragePath = Path.Combine(_webHostEnvironment.ContentRootPath, "Storage");
            _baseUrl = _configuration["FileStorage:BaseUrl"] ?? "/files";
        }

        public async Task<string> SaveFileAsync(IFormFile file, string folder)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("文件无效");
            }

            // 确保目录存在
            string dirPath = Path.Combine(_baseStoragePath, folder);
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }

            // 生成唯一文件名
            string fileExtension = Path.GetExtension(file.FileName);
            string fileName = $"{Guid.NewGuid()}{fileExtension}";
            string filePath = Path.Combine(dirPath, fileName);

            // 保存文件
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // 返回文件URL
            return GetFileUrl(fileName, folder);
        }

        public Task<bool> DeleteFileAsync(string fileUrl)
        {
            try
            {
                string filePath = GetFilePath(fileUrl);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    return Task.FromResult(true);
                }
                return Task.FromResult(false);
            }
            catch
            {
                return Task.FromResult(false);
            }
        }

        public string GetFileUrl(string fileName, string folder)
        {
            return $"{_baseUrl}/{folder}/{fileName}";
        }

        public string GetFilePath(string fileUrl)
        {
            // 从URL提取相对路径
            string relativePath = fileUrl.Replace(_baseUrl, "").TrimStart('/');

            // 转换为物理路径
            return Path.Combine(_baseStoragePath, relativePath);
        }

        public bool FileExists(string fileUrl)
        {
            if (string.IsNullOrEmpty(fileUrl))
            {
                return false;
            }

            string filePath = GetFilePath(fileUrl);
            return File.Exists(filePath);
        }
    }
}
