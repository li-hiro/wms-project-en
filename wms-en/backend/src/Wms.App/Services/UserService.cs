using Microsoft.EntityFrameworkCore;
using Wms.App.Data;
using Wms.App.Dtos;
using Wms.App.Entities;
using Wms.Core.Common;
using Wms.Core.Security;

namespace Wms.App.Services;

/// <summary>用户管理服务</summary>
public class UserService
{
    private readonly WmsDbContext _db;
    private readonly UserAccessService _access;
    private readonly ICurrentUser _currentUser;

    public UserService(WmsDbContext db, UserAccessService access, ICurrentUser currentUser)
    {
        _db = db;
        _access = access;
        _currentUser = currentUser;
    }

    /// <summary>分页查询用户</summary>
    public async Task<PagedResult<UserDto>> QueryAsync(UserQuery query, CancellationToken cancellationToken = default)
    {
        var source = _db.Users.AsQueryable();

        if (query.Status.HasValue)
            source = source.Where(x => x.Status == query.Status);

        if (query.RoleId.HasValue)
            source = source.Where(u => _db.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == query.RoleId));

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var keyword = query.Keyword.Trim();
            source = source.Where(x => x.UserName.Contains(keyword) || x.NickName.Contains(keyword));
        }

        var total = await source.LongCountAsync(cancellationToken);
        var users = await source
            .OrderByDescending(x => x.Id)
            .Skip(query.GetSkip())
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var userIds = users.Select(x => x.Id).ToList();
        var roleLinks = await (
            from ur in _db.UserRoles
            join r in _db.Roles on ur.RoleId equals r.Id
            where userIds.Contains(ur.UserId)
            select new { ur.UserId, RoleId = r.Id, r.Name }
        ).ToListAsync(cancellationToken);

        var items = users.Select(user => new UserDto
        {
            Id = user.Id,
            UserName = user.UserName,
            NickName = user.NickName,
            Email = user.Email,
            Phone = user.Phone,
            Status = user.Status,
            LastLoginAt = user.LastLoginAt,
            CreatedAt = user.CreatedAt,
            RoleIds = roleLinks.Where(x => x.UserId == user.Id).Select(x => x.RoleId).ToList(),
            RoleNames = roleLinks.Where(x => x.UserId == user.Id).Select(x => x.Name).ToList()
        }).ToList();

        return PagedResult<UserDto>.Create(items, total, query.Page, query.PageSize);
    }

    /// <summary>创建用户</summary>
    public async Task<long> CreateAsync(SaveUserRequest request, CancellationToken cancellationToken = default)
    {
        var userName = request.UserName.Trim();
        if (string.IsNullOrWhiteSpace(userName))
            throw new BusinessException("Username cannot be empty");

        if (await _db.Users.AnyAsync(x => x.UserName == userName, cancellationToken))
            throw new ConflictException($"Username {userName} already exists");

        if (string.IsNullOrWhiteSpace(request.Password))
            throw new BusinessException("Please set an initial password");

        var user = new User
        {
            UserName = userName,
            NickName = request.NickName?.Trim() ?? userName,
            PasswordHash = PasswordHasher.Hash(request.Password),
            Email = request.Email,
            Phone = request.Phone,
            Status = request.Status,
            CreatedBy = _currentUser.UserId
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);
        await ReplaceRolesAsync(user.Id, request.RoleIds, cancellationToken);
        return user.Id;
    }

    /// <summary>更新用户</summary>
    public async Task UpdateAsync(long id, SaveUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.FindAsync(new object[] { id }, cancellationToken)
                   ?? throw new NotFoundException("User not found");

        var userName = request.UserName.Trim();
        if (await _db.Users.AnyAsync(x => x.UserName == userName && x.Id != id, cancellationToken))
            throw new ConflictException($"Username {userName} already exists");

        user.UserName = userName;
        user.NickName = request.NickName?.Trim() ?? userName;
        user.Email = request.Email;
        user.Phone = request.Phone;
        user.Status = request.Status;
        user.UpdatedBy = _currentUser.UserId;

        if (!string.IsNullOrWhiteSpace(request.Password))
            user.PasswordHash = PasswordHasher.Hash(request.Password);

        await _db.SaveChangesAsync(cancellationToken);
        await ReplaceRolesAsync(id, request.RoleIds, cancellationToken);
    }

    /// <summary>删除用户（不允许删除自己与内置管理员）</summary>
    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        if (_currentUser.UserId == id)
            throw new BusinessException("Cannot delete the current signed-in account");

        var user = await _db.Users.FindAsync(new object[] { id }, cancellationToken)
                   ?? throw new NotFoundException("User not found");

        if (user.UserName == "admin")
            throw new BusinessException("The built-in administrator account cannot be deleted");

        _db.UserRoles.RemoveRange(_db.UserRoles.Where(x => x.UserId == id));
        _db.Users.Remove(user);
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>重置密码，返回新密码</summary>
    public async Task<string> ResetPasswordAsync(long id, string? newPassword, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.FindAsync(new object[] { id }, cancellationToken)
                   ?? throw new NotFoundException("User not found");

        var password = string.IsNullOrWhiteSpace(newPassword) ? PasswordHasher.GenerateRandom(10) : newPassword;
        user.PasswordHash = PasswordHasher.Hash(password);
        await _db.SaveChangesAsync(cancellationToken);
        return password;
    }

    /// <summary>启用 / 停用用户</summary>
    public async Task ToggleStatusAsync(long id, CommonStatus status, CancellationToken cancellationToken = default)
    {
        if (_currentUser.UserId == id)
            throw new BusinessException("Cannot change the status of the current signed-in account");

        var user = await _db.Users.FindAsync(new object[] { id }, cancellationToken)
                   ?? throw new NotFoundException("User not found");

        user.Status = status;
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task ReplaceRolesAsync(long userId, List<long> roleIds, CancellationToken cancellationToken)
    {
        var distinctRoleIds = roleIds.Distinct().ToList();
        var exists = await _db.Roles.Where(x => distinctRoleIds.Contains(x.Id)).Select(x => x.Id).ToListAsync(cancellationToken);
        var validRoleIds = distinctRoleIds.Where(exists.Contains).ToList();

        _db.UserRoles.RemoveRange(_db.UserRoles.Where(x => x.UserId == userId));
        foreach (var roleId in validRoleIds)
            _db.UserRoles.Add(new UserRole { UserId = userId, RoleId = roleId });

        await _db.SaveChangesAsync(cancellationToken);
    }
}
