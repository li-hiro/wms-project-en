using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Wms.Core.Security;

/// <summary>JWT 配置项，对应 appsettings.json 中的 Jwt 节点</summary>
public class JwtOptions
{
    /// <summary>签名密钥（生产环境务必通过环境变量覆盖）</summary>
    public string Secret { get; set; } = "wms-default-development-secret-key-change-me";

    /// <summary>签发者</summary>
    public string Issuer { get; set; } = "Wms";

    /// <summary>接收者</summary>
    public string Audience { get; set; } = "Wms.Client";

    /// <summary>令牌有效期（小时）</summary>
    public int ExpireHours { get; set; } = 12;
}

/// <summary>签发令牌所需的最小用户信息</summary>
public class TokenPayload
{
    public long UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string NickName { get; set; } = string.Empty;
    public IEnumerable<string> Roles { get; set; } = Array.Empty<string>();
    public IEnumerable<string> Permissions { get; set; } = Array.Empty<string>();
}

/// <summary>
/// JWT 令牌服务：负责签发访问令牌。
/// </summary>
public class JwtTokenService
{
    /// <summary>自定义声明：用户 ID</summary>
    public const string ClaimUserId = "uid";

    /// <summary>自定义声明：昵称</summary>
    public const string ClaimNickName = "nickname";

    /// <summary>自定义声明：角色</summary>
    public const string ClaimRole = "role";

    /// <summary>自定义声明：权限</summary>
    public const string ClaimPermission = "permission";

    private readonly JwtOptions _options;

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    /// <summary>签发令牌，返回令牌字符串与过期时间</summary>
    public (string Token, DateTime ExpiresAt) Create(TokenPayload payload)
    {
        var expiresAt = DateTime.Now.AddHours(_options.ExpireHours);
        var claims = new List<Claim>
        {
            new(ClaimUserId, payload.UserId.ToString()),
            new(ClaimTypes.Name, payload.UserName),
            new(ClaimNickName, payload.NickName ?? string.Empty)
        };
        claims.AddRange(payload.Roles.Select(role => new Claim(ClaimRole, role)));
        claims.AddRange(payload.Permissions.Select(code => new Claim(ClaimPermission, code)));

        var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(_options.Secret));
        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: DateTime.Now,
            expires: expiresAt,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
