using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using ToYouEMS.ToYouEMS.Core.Interfaces;
using ToYouEMS.ToYouEMS.Infrastructure.Data;
using ToYouEMS.ToYouEMS.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// 添加服务到容器

// 添加数据库上下文
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))
    )
);

// 添加依赖注入
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>(); // 添加文件存储服务
// 添加其他服务注册

// 添加控制器
builder.Services.AddControllers();

// 添加JWT身份验证
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])),
            // 添加以下配置，防止claims被自动映射
            NameClaimType = "unique_name",  // 确保用户名仍然可以通过 User.Identity.Name 访问
            RoleClaimType = "role"          // 角色声明
        };

        // 禁用自动映射，保留原始claim类型
        options.MapInboundClaims = false;
    });

// 添加Swagger/OpenAPI支持
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "ToYouEMS API", Version = "v1" });

    // 添加JWT认证支持到Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme."
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// 添加CORS策略
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        // 确保数据库被创建
        context.Database.EnsureCreated();
        Console.WriteLine("数据库创建成功");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "创建数据库时出错");
        Console.WriteLine($"创建数据库时出错: {ex.Message}");
    }
}

// 配置HTTP请求管道
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}

// 创建存储目录
var storagePath = Path.Combine(builder.Environment.ContentRootPath, "Storage");
if (!Directory.Exists(storagePath))
{
    Directory.CreateDirectory(storagePath);
    // 创建子目录
    Directory.CreateDirectory(Path.Combine(storagePath, "avatars"));
    Directory.CreateDirectory(Path.Combine(storagePath, "resumes"));
    Directory.CreateDirectory(Path.Combine(storagePath, "attendances"));
    Directory.CreateDirectory(Path.Combine(storagePath, "templates"));
}

//app.UseHttpsRedirection();
app.UseStaticFiles(); // 添加静态文件支持

// 添加文件访问中间件
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(Directory.GetCurrentDirectory(), "Storage")),
    RequestPath = "/Storage"
});

// 启用CORS
app.UseCors("AllowAll");

// 启用身份验证和授权
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// 初始化模板文件
await TemplateInitializer.InitializeTemplates(app.Services, builder.Environment);

app.Run();