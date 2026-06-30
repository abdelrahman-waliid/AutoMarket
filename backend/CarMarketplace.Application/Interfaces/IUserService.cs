using CarMarketplace.Application.DTOs;
using CarMarketplace.Domain.Enums;

namespace CarMarketplace.Application.Interfaces;

/// <summary>
/// Represents the business logic contract for user operations.
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Retrieves a user by their unique identifier asynchronously.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to retrieve.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the user DTO if found; otherwise, null.</returns>
    Task<UserDTO?> GetUserByIdAsync(Guid userId);

    /// <summary>
    /// Registers a new user in the system asynchronously.
    /// </summary>
    /// <param name="fullName">The full name of the user.</param>
    /// <param name="email">The email address of the user.</param>
    /// <param name="password">The password of the user (will be hashed before storage).</param>
    /// <param name="role">The role of the user.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the created user DTO.</returns>
    Task<UserDTO> RegisterAsync(string fullName, string email, string password, UserRole role);

    /// <summary>
    /// Updates an existing user in the system asynchronously. Role changes are applied only when <paramref name="currentUserRole"/> is Admin.
    /// </summary>
    /// <param name="userDto">The user DTO containing the updated user information.</param>
    /// <param name="currentUserRole">The role of the caller; only Admin can change a user's role.</param>
    /// <param name="currentUserId">The caller's user id from JWT claims.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the updated user DTO if found; otherwise, null.</returns>
    Task<UserDTO?> UpdateUserAsync(UserDTO userDto, UserRole currentUserRole, Guid currentUserId);

    /// <summary>
    /// Deletes a user from the system asynchronously.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to delete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result indicates whether the user was successfully deleted.</returns>
    Task<bool> DeleteUserAsync(Guid userId);

    /// <summary>
    /// Authenticates a user with their email and password asynchronously and returns API response + refresh token for cookie issuance.
    /// </summary>
    /// <param name="email">The email address of the user.</param>
    /// <param name="password">The password of the user.</param>
    /// <exception cref="CarMarketplace.Application.Exceptions.AccountLockedException">
    /// Thrown when the account is locked and cannot authenticate yet.
    /// </exception>
    /// <returns>A task that represents the asynchronous operation. The task result contains auth session info if credentials are valid; otherwise, null.</returns>
    Task<AuthSessionDTO?> AuthenticateAsync(string email, string password);

    /// <summary>
    /// Validates the refresh token and issues a new access token + rotated refresh token. Returns null if the refresh token is invalid.
    /// </summary>
    /// <param name="refreshToken">The refresh token value.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains auth session info (response + rotated refresh token) or null.</returns>
    Task<AuthSessionDTO?> RefreshAccessTokenAsync(string refreshToken);

    /// <summary>
    /// Revokes the given refresh token (e.g. on logout).
    /// </summary>
    /// <param name="refreshToken">The refresh token value to revoke.</param>
    /// <returns>A task that represents the asynchronous operation. The task result indicates whether a token was found and revoked.</returns>
    Task<bool> RevokeRefreshTokenAsync(string refreshToken);

    /// <summary>
    /// Retrieves all users from the system asynchronously.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of user DTOs.</returns>
    Task<List<UserDTO>> GetAllUsersAsync();

    /// <summary>
    /// Gets users as paginated data for admin panel.
    /// </summary>
    Task<PaginatedResult<UserDTO>> GetUsersPagedAsync(int pageNumber, int pageSize);

    /// <summary>
    /// Updates a user's role. Intended for admins.
    /// </summary>
    Task<UserDTO?> UpdateUserRoleAsync(Guid userId, UserRole role);

    /// <summary>
    /// Initiates forgot password flow using a one-time reset token.
    /// </summary>
    Task<string?> ForgotPasswordAsync(string email);

    /// <summary>
    /// Resets password using a valid reset token.
    /// </summary>
    Task ResetPasswordAsync(string token, string newPassword);

    /// <summary>
    /// Changes the authenticated user's password after verifying their current password.
    /// Returns false when the current password is invalid.
    /// </summary>
    Task<bool> ChangePasswordAsync(Guid currentUserId, string currentPassword, string newPassword);

    /// <summary>
    /// Marks a user online for chat presence.
    /// </summary>
    Task MarkUserOnlineAsync(Guid userId);

    /// <summary>
    /// Marks a user offline and returns the saved last-seen timestamp, or null when the user is missing.
    /// </summary>
    Task<DateTime?> MarkUserOfflineAsync(Guid userId);

    /// <summary>
    /// Gets current authenticated user's profile.
    /// </summary>
    Task<UserDTO?> GetProfileAsync(Guid currentUserId);

    /// <summary>
    /// Updates current authenticated user's profile.
    /// </summary>
    Task<UserDTO?> UpdateProfileAsync(Guid currentUserId, ProfileUpdateRequestDTO request);

    /// <summary>
    /// Updates current authenticated user's avatar URL.
    /// </summary>
    Task<UserDTO?> UpdateAvatarAsync(Guid currentUserId, string avatarUrl);
}
