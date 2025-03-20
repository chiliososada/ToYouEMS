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
        //分段上传技术


        public async Task<string> InitializeChunkedUploadAsync(string fileName, string folder)
        {
            // 确保临时目录存在
            string tempDir = Path.Combine(_baseStoragePath, "temp");
            if (!Directory.Exists(tempDir))
            {
                Directory.CreateDirectory(tempDir);
            }

            // 生成唯一的临时文件名
            string tempFileName = $"{Guid.NewGuid()}_{fileName}.temp";
            string tempFilePath = Path.Combine(tempDir, tempFileName);

            // 创建空文件
            using (var fs = File.Create(tempFilePath))
            {
                // 创建空文件即可
            }

            return tempFilePath;
        }

        public async Task AppendChunkAsync(string tempFilePath, Stream chunkStream, int chunkIndex)
        {
            // 确保临时文件存在
            if (!File.Exists(tempFilePath))
            {
                throw new FileNotFoundException("临时文件不存在", tempFilePath);
            }

            // 追加分片数据到临时文件
            using (var fileStream = new FileStream(tempFilePath, FileMode.Append))
            {
                await chunkStream.CopyToAsync(fileStream);
            }
        }

        public async Task<string> CompleteChunkedUploadAsync(string tempFilePath, string fileName, string folder)
        {
            // 确保临时文件存在
            if (!File.Exists(tempFilePath))
            {
                throw new FileNotFoundException("临时文件不存在", tempFilePath);
            }

            // 确保目标目录存在
            string dirPath = Path.Combine(_baseStoragePath, folder);
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }

            // 生成唯一文件名
            string fileExtension = Path.GetExtension(fileName);
            string finalFileName = $"{Guid.NewGuid()}{fileExtension}";
            string finalFilePath = Path.Combine(dirPath, finalFileName);

            // 移动文件
            File.Move(tempFilePath, finalFilePath);

            // 返回文件URL
            return GetFileUrl(finalFileName, folder);
        }
    }
}
