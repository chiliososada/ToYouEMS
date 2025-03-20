namespace ToYouEMS.ToYouEMS.Core.Interfaces
{
    public interface IFileStorageService
    {
        /// <summary>
        /// 保存用户上传的文件
        /// </summary>
        /// <param name="file">上传的文件</param>
        /// <param name="folder">存储文件夹</param>
        /// <returns>文件的访问路径</returns>
        Task<string> SaveFileAsync(IFormFile file, string folder);

        /// <summary>
        /// 删除文件
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>是否删除成功</returns>
        Task<bool> DeleteFileAsync(string filePath);

        /// <summary>
        /// 获取文件访问路径
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <param name="folder">文件夹名</param>
        /// <returns>访问路径</returns>
        string GetFileUrl(string fileName, string folder);

        /// <summary>
        /// 根据Url获取文件物理路径
        /// </summary>
        /// <param name="fileUrl">文件URL</param>
        /// <returns>文件物理路径</returns>
        string GetFilePath(string fileUrl);

        /// <summary>
        /// 检查文件是否存在
        /// </summary>
        /// <param name="fileUrl">文件URL</param>
        /// <returns>是否存在</returns>
        bool FileExists(string fileUrl);

        // 文件路径: ToYouEMS/ToYouEMS.Core/Interfaces/IFileStorageService.cs
        // 添加以下方法:

        /// <summary>
        /// 初始化分片上传，创建临时文件
        /// </summary>
        /// <param name="fileName">原始文件名</param>
        /// <param name="folder">目标文件夹</param>
        /// <returns>临时文件路径</returns>
        Task<string> InitializeChunkedUploadAsync(string fileName, string folder);

        /// <summary>
        /// 上传文件分片
        /// </summary>
        /// <param name="tempFilePath">临时文件路径</param>
        /// <param name="chunkStream">分片数据流</param>
        /// <param name="chunkIndex">分片索引</param>
        /// <returns>上传是否成功</returns>
        Task AppendChunkAsync(string tempFilePath, Stream chunkStream, int chunkIndex);

        /// <summary>
        /// 完成分片上传
        /// </summary>
        /// <param name="tempFilePath">临时文件路径</param>
        /// <param name="fileName">原始文件名</param>
        /// <param name="folder">目标文件夹</param>
        /// <returns>文件访问URL</returns>
        Task<string> CompleteChunkedUploadAsync(string tempFilePath, string fileName, string folder);
    }
}
