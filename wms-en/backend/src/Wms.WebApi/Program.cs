using System.Text;
using System.Text.Unicode;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Wms.App;
using Wms.App.Data;
using Wms.Core.Common;
using Wms.Core.Security;
using Wms.Im;
using Wms.Plugin;
using Wms.WebApi.Authorization;
using Wms.WebApi.Infrastructure;
using Wms.WebApi.Middleware;

var builder = WebApplication.CreateBuilder(args);

// ---------------- 基础服务 ----------------
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // 确保中文在 JSON 中不被转义，便于调试与前端展示
        options.JsonSerializerOptions.Encoder = JavaScriptEncoder.Create(UnicodeRanges.All);
    });

// 统一模型校验失败的响应格式
builder.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var messages = context.ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .Where(m => !string.IsNullOrWhiteSpace(m))
            .ToList();

        var payload = ApiResult<object>.Fail(
            ErrorCodes.BadRequest,
            messages.Count > 0 ? string.Join("; ", messages) : "Invalid request parameters");

        return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(payload);
    };
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "WMS Warehouse Management System API",
        Version = "v1.0.0",
        Description = "API documentation for a warehouse management system with a decoupled front-end/back-end architecture. Modules include: Master Data, Warehouse Configuration, Inbound, Outbound, Stocktake, Transfer, Inventory, Reports, and System Permissions."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Enter your JWT token in the format: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// ---------------- 模块装配 ----------------
builder.Services.AddApplicationModule(builder.Configuration);
builder.Services.AddInventoryModule();
builder.Services.AddPluginModule();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddSingleton<JwtTokenService>();

// ---------------- 认证授权 ----------------
var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = System.Security.Claims.ClaimTypes.Name,
            RoleClaimType = JwtTokenService.ClaimRole
        };
        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();
                if (context.Response.HasStarted) return;

                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json; charset=utf-8";
                var payload = ApiResult<object>.Fail(ErrorCodes.Unauthorized, "Not signed in or your session has expired");
                payload.TraceId = context.HttpContext.TraceIdentifier;
                await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(payload));
            },
            OnForbidden = async context =>
            {
                if (context.Response.HasStarted) return;

                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json; charset=utf-8";
                var payload = ApiResult<object>.Fail(ErrorCodes.Forbidden, "You do not have permission to access this resource");
                payload.TraceId = context.HttpContext.TraceIdentifier;
                await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(payload));
            }
        };
    });

builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.AddAuthorization(options =>
{
    // 默认所有接口都需要登录，个别接口通过 [AllowAnonymous] 放行
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// ---------------- 跨域 ----------------
builder.Services.AddCors(options =>
{
    options.AddPolicy("Default", policy => policy
        .WithOrigins("http://localhost:8080", "http://127.0.0.1:8080", "http://localhost:5173")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

var app = builder.Build();

// ---------------- 中间件管线 ----------------
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "WMS API v1");
    options.DocumentTitle = "WMS API Documentation";
});
app.UseCors("Default");
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<OperationLogMiddleware>();
app.MapControllers();

app.MapGet("/", () => Results.Redirect("/swagger")).AllowAnonymous();
app.MapGet("/api/health", () => ApiResult<object>.Ok(new
{
    status = "UP",
    service = "WMS",
    time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
})).AllowAnonymous();

// ---------------- 数据初始化与插件加载 ----------------
await DbInitializer.InitializeAsync(app.Services);

var pluginDirectory = Path.Combine(app.Environment.ContentRootPath, "Plugins");
app.Services.GetRequiredService<PluginManager>().LoadExternalPlugins(pluginDirectory);

app.Run();
