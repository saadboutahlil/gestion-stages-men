using Microsoft.AspNetCore.Identity;

namespace GestionStagesMEN.Core.Entities;

/// <summary>
/// Utilisateur de l'application — hérite d'IdentityUser pour l'authentification.
/// </summary>
public class ApplicationUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
