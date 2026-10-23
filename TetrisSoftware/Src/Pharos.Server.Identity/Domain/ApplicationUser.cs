using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace Pharos.Server.Identity.Domain;

public class ApplicationUser : IdentityUser<Guid>
{
    public const int FullNameLength = 100;
    public const int PinLength = 4;

    public DateTimeOffset? LastLoginDateTimeOffset { get; set; }
    public bool IsActive { get; set; } = true;
    public bool TemporaryPassword { get; set; } = true;

    /// <summary>
    /// Gets or sets the date and time when the current password was created or last changed.
    /// Expiration is calculated dynamically as this value plus the configured PasswordExpirationDays.
    /// </summary>
    public DateTimeOffset? PasswordEmittedDateTimeOffset { get; set; }
    /// <summary>
    /// Gets the full person's name.
    /// </summary>
    [ProtectedPersonalData]
    [MaxLength(FullNameLength)]
    public string? FullName { get; set; }

    /// <summary>
    /// Gets the user role
    /// </summary>
    [NotMapped]
    public ApplicationRole? Role { get; set; }

    /// <summary>
    /// Gets or sets the hashed PIN code for authentication. Stored as a hash, not plain text.
    /// </summary>
    [MaxLength(256)]
    public string? PinHash { get; set; }

    /// <summary>
    /// Gets or sets whether PIN authentication is enabled for this user.
    /// </summary>
    public bool IsPinEnabled { get; set; }

    /// <summary>
    /// Gets a value indicating whether the user needs to set a new PIN.
    /// </summary>
    public bool NeedsNewPin => IsActive && IsPinEnabled && string.IsNullOrEmpty(PinHash);
}