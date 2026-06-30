using CarMarketplace.Application.Configuration;
using CarMarketplace.Application.DTOs;
using CarMarketplace.Application.Exceptions;
using CarMarketplace.Application.Interfaces;
using CarMarketplace.Application.Security;
using CarMarketplace.Domain.Entities;
using CarMarketplace.Domain.Enums;
using System.Security.Cryptography;
using System.Text;

namespace CarMarketplace.Application.Services;

/// <summary>
/// Service implementation for user operations.
/// </summary>
public class UserService : IUserService
{
    private const int MaxFailedLoginAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan PasswordResetTokenValidity = TimeSpan.FromMinutes(30);
    private const string RefreshTokenRevokedByRotation = "Rotated";
    private const string RefreshTokenRevokedByReuse = "ReuseDetected";
    private const string RefreshTokenRevokedByLogout = "Logout";
    private const string RefreshTokenRevokedByExpiry = "Expired";
    private const string RefreshTokenRevokedByPasswordReset = "PasswordReset";
    private const string RefreshTokenRevokedByPasswordChange = "PasswordChange";
    private const string RefreshTokenRevokedByUserDeletion = "UserDeleted";
    private const string RefreshTokenRevokedByReregistration = "ReRegistered";

    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordResetTokenRepository _passwordResetTokenRepository;
    private readonly IRefreshTokenGenerator _refreshTokenGenerator;
    private readonly ITokenService _tokenService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRefreshTokenSettings _refreshTokenSettings;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserService"/> class.
    /// </summary>
    public UserService(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordResetTokenRepository passwordResetTokenRepository,
        IRefreshTokenGenerator refreshTokenGenerator,
        ITokenService tokenService,
        IUnitOfWork unitOfWork,
        IRefreshTokenSettings refreshTokenSettings)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordResetTokenRepository = passwordResetTokenRepository;
        _refreshTokenGenerator = refreshTokenGenerator;
        _tokenService = tokenService;
        _unitOfWork = unitOfWork;
        _refreshTokenSettings = refreshTokenSettings;
    }

    /// <inheritdoc/>
    public async Task<UserDTO?> GetUserByIdAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        return user == null ? null : MapToDTO(user);
    }

    /// <inheritdoc/>
    public async Task<UserDTO> RegisterAsync(string fullName, string email, string password, UserRole role)
    {
        var existingUser = await _userRepository.GetByEmailIncludingDeletedTrackedAsync(email);
        if (existingUser != null && !existingUser.IsDeleted)
        {
            throw new InvalidOperationException($"User with email '{email}' already exists.");
        }

        ValidatePasswordPolicyOrThrow(password);

        var now = DateTime.UtcNow;
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt(12));

        if (existingUser != null)
        {
            existingUser.IsDeleted = false;
            existingUser.FullName = fullName;
            existingUser.Email = email;
            existingUser.PasswordHash = passwordHash;
            existingUser.SecurityStamp = CreateSecurityStamp();
            existingUser.Role = role;
            existingUser.FailedLoginAttempts = 0;
            existingUser.LockoutEnd = null;
            existingUser.IsOnline = false;
            existingUser.LastSeen = null;
            existingUser.UpdatedAt = now;

            await _refreshTokenRepository.RevokeAllActiveForUserAsync(existingUser.Id, RefreshTokenRevokedByReregistration);
            await _unitOfWork.SaveChangesAsync();
            return MapToDTO(existingUser);
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = fullName,
            Email = email,
            PasswordHash = passwordHash,
            SecurityStamp = CreateSecurityStamp(),
            AvatarUrl = null,
            FailedLoginAttempts = 0,
            LockoutEnd = null,
            IsOnline = false,
            LastSeen = null,
            Role = role,
            CreatedAt = now,
            UpdatedAt = now
        };

        _userRepository.Add(user);
        await _unitOfWork.SaveChangesAsync();

        return MapToDTO(user);
    }

    /// <inheritdoc/>
    public async Task<UserDTO?> UpdateUserAsync(UserDTO userDto, UserRole currentUserRole, Guid currentUserId)
    {
        if (currentUserId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("Authentication is required.");
        }

        if (currentUserRole != UserRole.Admin && userDto.Id != currentUserId)
        {
            throw new ForbiddenAccessException("You can only update your own profile.");
        }

        var user = await _userRepository.GetByIdTrackedAsync(userDto.Id);
        if (user == null)
        {
            return null;
        }

        if (user.Email != userDto.Email)
        {
            if (await _userRepository.ExistsByEmailAsync(userDto.Email, userDto.Id))
            {
                throw new InvalidOperationException($"User with email '{userDto.Email}' already exists.");
            }
        }

        user.FullName = userDto.FullName;
        user.Email = userDto.Email;
        user.UpdatedAt = DateTime.UtcNow;

        if (currentUserRole == UserRole.Admin)
        {
            user.Role = userDto.Role;
        }

        await _unitOfWork.SaveChangesAsync();

        return MapToDTO(user);
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteUserAsync(Guid userId)
    {
        var deleted = await _userRepository.SoftDeleteCascadeAsync(userId, DateTime.UtcNow);
        if (!deleted)
        {
            return false;
        }

        await _refreshTokenRepository.RevokeAllActiveForUserAsync(userId, RefreshTokenRevokedByUserDeletion);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    /// <inheritdoc/>
    public async Task<AuthSessionDTO?> AuthenticateAsync(string email, string password)
    {
        var user = await _userRepository.GetByEmailTrackedAsync(email);
        if (user == null)
        {
            return null;
        }

        var now = DateTime.UtcNow;

        if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > now)
        {
            throw new AccountLockedException(user.LockoutEnd.Value);
        }

        if (user.LockoutEnd.HasValue && user.LockoutEnd.Value <= now)
        {
            user.FailedLoginAttempts = 0;
            user.LockoutEnd = null;
        }

        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            user.FailedLoginAttempts++;
            user.UpdatedAt = now;

            if (user.FailedLoginAttempts >= MaxFailedLoginAttempts)
            {
                user.LockoutEnd = now.Add(LockoutDuration);
            }

            await _unitOfWork.SaveChangesAsync();
            return null;
        }

        user.FailedLoginAttempts = 0;
        user.LockoutEnd = null;
        user.UpdatedAt = now;

        var userDto = MapToDTO(user);
        var accessToken = _tokenService.GenerateToken(userDto);

        var refreshTokenValue = _refreshTokenGenerator.Generate();
        _refreshTokenRepository.Add(CreateRefreshTokenEntity(user.Id, refreshTokenValue));
        await _unitOfWork.SaveChangesAsync();

        return new AuthSessionDTO
        {
            Response = new LoginResponseDTO
            {
                User = userDto,
                Token = accessToken
            },
            RefreshToken = refreshTokenValue
        };
    }

    /// <inheritdoc/>
    public async Task<AuthSessionDTO?> RefreshAccessTokenAsync(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return null;

        var tokenHash = HashRefreshToken(refreshToken);
        var tokenEntity = await _refreshTokenRepository.GetByTokenHashTrackedAsync(tokenHash);
        if (tokenEntity == null)
            return null;

        var now = DateTime.UtcNow;

        if (tokenEntity.IsRevoked)
        {
            if (IsReuseAttempt(tokenEntity))
            {
                await _refreshTokenRepository.RevokeAllActiveForUserAsync(tokenEntity.UserId, RefreshTokenRevokedByReuse);
                tokenEntity.RevokedAt ??= now;
                tokenEntity.RevokedReason ??= RefreshTokenRevokedByReuse;
                await _unitOfWork.SaveChangesAsync();
                throw new RefreshTokenReuseDetectedException(
                    "Refresh token reuse detected. All active refresh tokens have been revoked.");
            }

            return null;
        }

        if (tokenEntity.ExpiryDate <= now)
        {
            tokenEntity.IsRevoked = true;
            tokenEntity.RevokedAt = now;
            tokenEntity.RevokedReason = RefreshTokenRevokedByExpiry;
            await _unitOfWork.SaveChangesAsync();
            return null;
        }

        var user = await _userRepository.GetByIdAsync(tokenEntity.UserId);
        if (user == null)
        {
            tokenEntity.IsRevoked = true;
            tokenEntity.RevokedAt = now;
            tokenEntity.RevokedReason = "UserNotFound";
            await _unitOfWork.SaveChangesAsync();
            return null;
        }

        var userDto = MapToDTO(user);
        var accessToken = _tokenService.GenerateToken(userDto);
        var newRefreshTokenValue = _refreshTokenGenerator.Generate();
        var newRefreshTokenHash = HashRefreshToken(newRefreshTokenValue);

        tokenEntity.IsRevoked = true;
        tokenEntity.RevokedAt = now;
        tokenEntity.RevokedReason = RefreshTokenRevokedByRotation;
        tokenEntity.ReplacedByTokenHash = newRefreshTokenHash;

        _refreshTokenRepository.Add(CreateRefreshTokenEntity(user.Id, newRefreshTokenValue, newRefreshTokenHash));
        await _unitOfWork.SaveChangesAsync();

        return new AuthSessionDTO
        {
            Response = new LoginResponseDTO
            {
                User = userDto,
                Token = accessToken
            },
            RefreshToken = newRefreshTokenValue
        };
    }

    /// <inheritdoc/>
    public async Task<bool> RevokeRefreshTokenAsync(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return false;

        var tokenHash = HashRefreshToken(refreshToken);
        var revoked = await _refreshTokenRepository.RevokeByTokenHashAsync(tokenHash, RefreshTokenRevokedByLogout);
        if (revoked)
            await _unitOfWork.SaveChangesAsync();
        return revoked;
    }

    /// <inheritdoc/>
    public async Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword)
    {
        if (userId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("Authentication is required.");
        }

        if (string.IsNullOrWhiteSpace(currentPassword))
        {
            return false;
        }

        ValidatePasswordPolicyOrThrow(newPassword);

        if (currentPassword == newPassword)
        {
            throw new InvalidOperationException("New password must be different from the current password.");
        }

        var user = await _userRepository.GetByIdTrackedAsync(userId);
        if (user == null)
        {
            throw new KeyNotFoundException("User not found.");
        }

        if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
        {
            return false;
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword, BCrypt.Net.BCrypt.GenerateSalt(12));
        user.SecurityStamp = CreateSecurityStamp();
        user.FailedLoginAttempts = 0;
        user.LockoutEnd = null;
        user.UpdatedAt = DateTime.UtcNow;

        await _refreshTokenRepository.RevokeAllActiveForUserAsync(user.Id, RefreshTokenRevokedByPasswordChange);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    /// <inheritdoc/>
    public async Task<List<UserDTO>> GetAllUsersAsync()
    {
        var users = await _userRepository.GetAllAsync();
        return users.Select(MapToDTO).ToList();
    }

    /// <inheritdoc/>
    public async Task<PaginatedResult<UserDTO>> GetUsersPagedAsync(int pageNumber, int pageSize)
    {
        var normalizedPageNumber = pageNumber < 1 ? 1 : pageNumber;
        var normalizedPageSize = pageSize < 1 ? 10 : Math.Min(pageSize, 100);
        var skip = (normalizedPageNumber - 1) * normalizedPageSize;
        var totalCount = await _userRepository.CountAsync();
        var users = await _userRepository.GetPagedAsync(skip, normalizedPageSize);

        return new PaginatedResult<UserDTO>
        {
            Items = users.Select(MapToDTO).ToList(),
            TotalCount = totalCount,
            PageNumber = normalizedPageNumber,
            PageSize = normalizedPageSize
        };
    }

    /// <inheritdoc/>
    public async Task<UserDTO?> UpdateUserRoleAsync(Guid userId, UserRole role)
    {
        var user = await _userRepository.GetByIdTrackedAsync(userId);
        if (user == null)
        {
            return null;
        }

        user.Role = role;
        user.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync();
        return MapToDTO(user);
    }

    /// <inheritdoc/>
    public async Task<string?> ForgotPasswordAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var user = await _userRepository.GetByEmailTrackedAsync(email);
        if (user == null)
        {
            return null;
        }

        await _passwordResetTokenRepository.InvalidateActiveTokensForUserAsync(user.Id);

        var rawToken = GenerateOpaqueToken();
        var token = new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = HashRefreshToken(rawToken),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.Add(PasswordResetTokenValidity),
            IsUsed = false
        };

        _passwordResetTokenRepository.Add(token);
        await _unitOfWork.SaveChangesAsync();

        return rawToken;
    }

    /// <inheritdoc/>
    public async Task ResetPasswordAsync(string token, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException("Reset token is required.");
        }

        ValidatePasswordPolicyOrThrow(newPassword);

        var tokenHash = HashRefreshToken(token);
        var tokenEntity = await _passwordResetTokenRepository.GetByTokenHashTrackedAsync(tokenHash);
        if (tokenEntity == null || tokenEntity.IsUsed || tokenEntity.ExpiresAt <= DateTime.UtcNow)
        {
            throw new InvalidOperationException("Invalid or expired password reset token.");
        }

        var user = await _userRepository.GetByIdTrackedAsync(tokenEntity.UserId);
        if (user == null)
        {
            throw new KeyNotFoundException("User not found for password reset.");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword, BCrypt.Net.BCrypt.GenerateSalt(12));
        user.SecurityStamp = CreateSecurityStamp();
        user.FailedLoginAttempts = 0;
        user.LockoutEnd = null;
        user.UpdatedAt = DateTime.UtcNow;

        tokenEntity.IsUsed = true;
        await _passwordResetTokenRepository.InvalidateActiveTokensForUserAsync(user.Id);
        await _refreshTokenRepository.RevokeAllActiveForUserAsync(user.Id, RefreshTokenRevokedByPasswordReset);
        await _unitOfWork.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task MarkUserOnlineAsync(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            return;
        }

        var user = await _userRepository.GetByIdTrackedAsync(userId);
        if (user == null)
        {
            return;
        }

        user.IsOnline = true;
        user.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task<DateTime?> MarkUserOfflineAsync(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            return null;
        }

        var user = await _userRepository.GetByIdTrackedAsync(userId);
        if (user == null)
        {
            return null;
        }

        var now = DateTime.UtcNow;
        user.IsOnline = false;
        user.LastSeen = now;
        user.UpdatedAt = now;
        await _unitOfWork.SaveChangesAsync();
        return now;
    }

    /// <inheritdoc/>
    public async Task<UserDTO?> GetProfileAsync(Guid currentUserId)
    {
        if (currentUserId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("Authentication is required.");
        }

        var user = await _userRepository.GetByIdAsync(currentUserId);
        return user == null ? null : MapToDTO(user);
    }

    /// <inheritdoc/>
    public async Task<UserDTO?> UpdateProfileAsync(Guid currentUserId, ProfileUpdateRequestDTO request)
    {
        if (currentUserId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("Authentication is required.");
        }

        var user = await _userRepository.GetByIdTrackedAsync(currentUserId);
        if (user == null)
        {
            return null;
        }

        var normalizedEmail = request.Email.Trim();
        if (!string.Equals(user.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase)
            && await _userRepository.ExistsByEmailAsync(normalizedEmail, user.Id))
        {
            throw new InvalidOperationException($"User with email '{request.Email}' already exists.");
        }

        user.FullName = request.FullName.Trim();
        user.Email = normalizedEmail;
        user.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync();
        return MapToDTO(user);
    }

    /// <inheritdoc/>
    public async Task<UserDTO?> UpdateAvatarAsync(Guid currentUserId, string avatarUrl)
    {
        if (currentUserId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("Authentication is required.");
        }

        var user = await _userRepository.GetByIdTrackedAsync(currentUserId);
        if (user == null)
        {
            return null;
        }

        user.AvatarUrl = avatarUrl;
        user.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync();
        return MapToDTO(user);
    }

    private static UserDTO MapToDTO(User user)
    {
        return new UserDTO
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            AvatarUrl = user.AvatarUrl,
            IsOnline = user.IsOnline,
            LastSeen = user.LastSeen,
            SecurityStamp = user.SecurityStamp,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }

    private RefreshToken CreateRefreshTokenEntity(Guid userId, string refreshTokenValue, string? precomputedHash = null)
    {
        var now = DateTime.UtcNow;
        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = precomputedHash ?? HashRefreshToken(refreshTokenValue),
            CreatedAt = now,
            ExpiresAt = now.AddDays(_refreshTokenSettings.RefreshTokenExpirationDays),
            IsRevoked = false
        };
    }

    private static bool IsReuseAttempt(RefreshToken tokenEntity)
    {
        return !string.IsNullOrWhiteSpace(tokenEntity.ReplacedByTokenHash)
               || string.Equals(tokenEntity.RevokedReason, RefreshTokenRevokedByRotation, StringComparison.Ordinal)
               || string.Equals(tokenEntity.RevokedReason, RefreshTokenRevokedByReuse, StringComparison.Ordinal);
    }

    private static string HashRefreshToken(string refreshToken)
    {
        var bytes = Encoding.UTF8.GetBytes(refreshToken);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }

    private static string GenerateOpaqueToken()
    {
        Span<byte> bytes = stackalloc byte[48];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static string CreateSecurityStamp()
    {
        return Guid.NewGuid().ToString("N");
    }

    private static void ValidatePasswordPolicyOrThrow(string password)
    {
        var passwordErrors = PasswordPolicy.Validate(password);
        if (passwordErrors.Count > 0)
        {
            throw new InvalidOperationException(string.Join(" ", passwordErrors));
        }
    }
}
