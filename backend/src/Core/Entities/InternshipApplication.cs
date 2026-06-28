using GestionStagesMEN.Core.Enums;

namespace GestionStagesMEN.Core.Entities;

/// <summary>
/// Candidature d'un étudiant à une offre de stage.
/// </summary>
public class InternshipApplication
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid StudentId { get; set; }
    public Student Student { get; set; } = null!;

    public Guid OfferId { get; set; }
    public InternshipOffer Offer { get; set; } = null!;

    public ApplicationStatus Statut { get; set; } = ApplicationStatus.Soumise;
    public DateTime DatePostulation { get; set; } = DateTime.UtcNow;

    public string? CvPath { get; set; }
    public string? LettreMotivationPath { get; set; }
    public string? Message { get; set; }
    public string? MotifRefus { get; set; }

    // Navigation
    public InternshipAgreement? Agreement { get; set; }
}
