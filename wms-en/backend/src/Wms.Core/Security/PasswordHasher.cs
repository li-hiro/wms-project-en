using System.Security.Cryptography;
using System.Text;

namespace Wms.Core.Security;

/// <summary>
/// 基于 PBKDF2-SHA256 的密码散列工具，避免明文存储与可逆加密。
/// </summary>
public static class PasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 100_000;
    private const string Prefix = "pbkdf2";

    /// <summary>生成密码散列，格式：pbkdf2$迭代次数$盐$散列</summary>
    public static string Hash(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be empty", nameof(password));

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
        return $"{Prefix}${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    /// <summary>校验密码是否匹配</summary>
    public static bool Verify(string password, string hashed)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hashed))
            return false;

        var parts = hashed.Split('$');
        if (parts.Length != 4 || parts[0] != Prefix)
            return false;

        var iterations = int.Parse(parts[1]);
        var salt = Convert.FromBase64String(parts[2]);
        var expected = Convert.FromBase64String(parts[3]);
        var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    /// <summary>生成指定长度的随机密码（用于初始化管理员账号）</summary>
    public static string GenerateRandom(int length = 12)
    {
        const string chars = "abcdefghijkmnpqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var bytes = RandomNumberGenerator.GetBytes(length);
        var builder = new StringBuilder(length);
        foreach (var b in bytes)
            builder.Append(chars[b % chars.Length]);
        return builder.ToString();
    }
}
