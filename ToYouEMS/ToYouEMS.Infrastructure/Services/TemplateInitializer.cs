namespace ToYouEMS.ToYouEMS.Infrastructure.Services
{
    public class TemplateInitializer
    {
        public static async Task InitializeTemplates(IServiceProvider serviceProvider, IWebHostEnvironment environment)
        {
            using var scope = serviceProvider.CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<TemplateInitializer>>();

            try
            {
                // 模板存储目录
                var templatesPath = Path.Combine(environment.ContentRootPath, "Storage", "templates");

                // 确保目录存在
                if (!Directory.Exists(templatesPath))
                {
                    Directory.CreateDirectory(templatesPath);
                }

                // 创建简历模板（这里只是创建一个空白文件作为示例）
                await CreateTemplateFileIfNotExists(templatesPath, "resume_template.xlsx", logger);

                // 创建勤务表模板（这里只是创建一个空白文件作为示例）
                await CreateTemplateFileIfNotExists(templatesPath, "attendance_template.xlsx", logger);

                logger.LogInformation("模板文件初始化完成");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "初始化模板文件时出错");
            }
        }

        private static async Task CreateTemplateFileIfNotExists(string directory, string fileName, ILogger logger)
        {
            var filePath = Path.Combine(directory, fileName);

            if (!File.Exists(filePath))
            {
                // 实际项目中，这里应该从嵌入的资源或其他位置复制实际的模板文件
                // 这里只是创建一个空白文件作为示例
                using (var fs = File.Create(filePath))
                {
                    // 如果是Excel文件，可以使用EPPlus或NPOI等库创建有格式的模板
                    // 这里只是创建一个空白文件
                }

                logger.LogInformation($"创建模板文件: {fileName}");
            }
        }
    }
}
