using System.Security.Cryptography;
using DijitalMenu.Domain;

namespace DijitalMenu.Application;

public interface IPasswordService
{
    string Hash(string password);
    bool Verify(string password, string passwordHash);
}

public interface IAuthenticationService
{
    AuthenticatedStaffDto? Authenticate(string username, string password);
}

public interface IStaffService
{
    IReadOnlyList<StaffUser> GetUsers();
    StaffUser? GetUser(int id);
    StaffUser Save(int? id, string username, string displayName, StaffRole role, bool isActive, string? password);
}

public sealed record AuthenticatedStaffDto(int Id, int BranchId, string Username, string DisplayName, StaffRole Role);

public sealed class PasswordService : IPasswordService
{
    private const int Iterations = 100_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
        return $"v1.{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public bool Verify(string password, string passwordHash)
    {
        var parts = passwordHash.Split('.');
        if (parts.Length != 4 ||
            parts[0] != "v1" ||
            !int.TryParse(parts[1], out var iterations))
        {
            return false;
        }

        try
        {
            var salt = Convert.FromBase64String(parts[2]);
            var expectedHash = Convert.FromBase64String(parts[3]);
            var actualHash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                iterations,
                HashAlgorithmName.SHA256,
                expectedHash.Length);

            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}

public sealed class AuthenticationService(
    IRestaurantRepository repository,
    IPasswordService passwordService) : IAuthenticationService
{
    public AuthenticatedStaffDto? Authenticate(string username, string password)
    {
        var user = repository.GetStaffUser(username.Trim().ToLowerInvariant());
        if (user is not { IsActive: true } || !passwordService.Verify(password, user.PasswordHash))
        {
            return null;
        }

        return new AuthenticatedStaffDto(user.Id, user.BranchId, user.Username, user.DisplayName, user.Role);
    }
}

public sealed class StaffService(
    IRestaurantRepository repository,
    IPasswordService passwordService) : IStaffService
{
    public IReadOnlyList<StaffUser> GetUsers() => repository.GetStaffUsers();

    public StaffUser? GetUser(int id) => repository.GetStaffUser(id);

    public StaffUser Save(
        int? id,
        string username,
        string displayName,
        StaffRole role,
        bool isActive,
        string? password)
    {
        var normalizedUsername = username.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalizedUsername) || string.IsNullOrWhiteSpace(displayName))
        {
            throw new InvalidOperationException("Kullanıcı adı ve görünen ad zorunludur.");
        }

        var duplicate = repository.GetStaffUser(normalizedUsername);
        if (duplicate is not null && duplicate.Id != id)
        {
            throw new InvalidOperationException("Bu kullanıcı adı zaten kullanılıyor.");
        }

        var user = id.HasValue
            ? repository.GetStaffUser(id.Value) ?? throw new InvalidOperationException("Personel bulunamadı.")
            : new StaffUser
            {
                Username = normalizedUsername,
                DisplayName = displayName.Trim(),
                PasswordHash = string.Empty
            };

        if (!string.IsNullOrWhiteSpace(password))
        {
            if (password.Length < 6)
            {
                throw new InvalidOperationException("Parola en az 6 karakter olmalıdır.");
            }

            user.PasswordHash = passwordService.Hash(password);
        }

        if (string.IsNullOrEmpty(user.PasswordHash))
        {
            throw new InvalidOperationException("Yeni personel için parola zorunludur.");
        }

        if (user.Role == StaffRole.Admin &&
            (!isActive || role != StaffRole.Admin) &&
            !repository.GetStaffUsers().Any(other =>
                other.Id != user.Id &&
                other.IsActive &&
                other.Role == StaffRole.Admin))
        {
            throw new InvalidOperationException("Son aktif yönetici pasife alınamaz veya rolü değiştirilemez.");
        }

        user.Username = normalizedUsername;
        user.DisplayName = displayName.Trim();
        user.Role = role;
        user.IsActive = isActive;

        if (!id.HasValue)
        {
            return repository.AddStaffUser(user);
        }

        repository.UpdateStaffUser(user);
        return user;
    }
}
