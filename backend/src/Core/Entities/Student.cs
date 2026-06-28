namespace GestionStagesMEN.Core.Entities;

/// <summary>
/// Profil étudiant/stagiaire, lié 1:1 à un ApplicationUser.
/// </summary>
public class Student
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public string CNE { get; set; } = string.Empty;          // Code National Étudiant
    public string Filiere { get; set; } = string.Empty;
    public string Promotion { get; set; } = string.Empty;    // ex: 3ème année GI
    public string? Etablissement { get; set; }                // ex: ENSIAS, EMI

    public string? CvFilePath { get; set; }
    public string? LettreMotivationPath { get; set; }

    // Navigation
    public List<InternshipApplication> Applications { get; set; } = new();
}
