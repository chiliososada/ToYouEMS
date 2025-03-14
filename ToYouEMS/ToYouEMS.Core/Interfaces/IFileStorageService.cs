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
    }
}
