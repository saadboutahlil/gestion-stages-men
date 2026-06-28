namespace GestionStagesMEN.Core.Entities;

/// <summary>
/// Profil établissement d'enseignement (École/Université), lié 1:1 à un ApplicationUser.
/// </summary>
public class School
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public string NomEtablissement { get; set; } = string.Empty;
    public string? Adresse { get; set; }
    public string? Telephone { get; set; }
    public string? EmailContact { get; set; }
}
