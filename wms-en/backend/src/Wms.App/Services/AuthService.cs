using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Wms.App.Data;
using Wms.App.Dtos;
using Wms.Core.Common;
using Wms.Core.Security;

namespace Wms.App.Services;

/// <summary>认证服务：登录、个人信息与密码修改</summary>
public class AuthService
{
    private readonly WmsDbContext _db;
    private readonly JwtTokenService _tokenService;
    private readonly UserAccessService _access;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        WmsDbContext db,
        JwtTokenService tokenService,
        UserAccessService access,
        ILogger<AuthService> logger)
    {
        _db = db;
        _tokenService = tokenService;
        _access = access;
        _logger = logger;
    }

    /// <summary>账号密码登录，成功后签发 JWT</summary>
    public async Task<LoginResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var userName = request.UserName.Trim();
        var user = await _db.Users.FirstOrDefaultAsync(x => x.UserName == userName, cancellationToken);

        if (user == null || !PasswordHasher.Verify(request.Password, user.PasswordHash))
            throw new BusinessException("Incorrect username or password", ErrorCodes.Unauthorized);

        if (user.Status != CommonStatus.Enabled)
            throw new BusinessException("This account has been disabled. Contact the administrator", ErrorCodes.Forbidden);

        var roles = await _access.GetRolesAsync(user.Id, cancellationToken);
        var permissions = await _access.GetPermissionCodesAsync(user.Id, cancellationToken);

        var (token, expiresAt) = _tokenService.Create(new TokenPayload
        {
            UserId = user.Id,
            UserName = user.UserName,
            NickName = user.NickName,
            Roles = roles.Select(x => x.Code),
            Permissions = permissions
        });

        user.LastLoginAt = DateTime.Now;
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("User {UserName} logged in", user.UserName);

        return new LoginResult
        {
            Token = token,
            ExpiresAt = expiresAt,
            User = new UserProfileDto
            {
                Id = user.Id,
                UserName = user.UserName,
                NickName = user.NickName,
                Email = user.Email,
                Phone = user.Phone,
                Roles = roles.Select(x => x.Code).ToList(),
                RoleNames = roles.Select(x => x.Name).ToList(),
                Permissions = permissions
            }
        };
    }

    /// <summary>获取当前用户信息</summary>
    public async Task<UserProfileDto> GetProfileAsync(long userId, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.FindAsync(new object[] { userId }, cancellationToken)
                   ?? throw new NotFoundException("User not found");

        var roles = await _access.GetRolesAsync(userId, cancellationToken);
        var permissions = await _access.GetPermissionCodesAsync(userId, cancellationToken);

        return new UserProfileDto
        {
            Id = user.Id,
            UserName = user.UserName,
            NickName = user.NickName,
            Email = user.Email,
            Phone = user.Phone,
            Roles = roles.Select(x => x.Code).ToList(),
            RoleNames = roles.Select(x => x.Name).ToList(),
            Permissions = permissions
        };
    }

    /// <summary>修改当前用户密码</summary>
    public async Task ChangePasswordAsync(long userId, ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.FindAsync(new object[] { userId }, cancellationToken)
                   ?? throw new NotFoundException("User not found");

        if (!PasswordHasher.Verify(request.OldPassword, user.PasswordHash))
            throw new BusinessException("Incorrect current password");

        if (request.OldPassword == request.NewPassword)
            throw new BusinessException("New password must differ from the current password");

        user.PasswordHash = PasswordHasher.Hash(request.NewPassword);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
