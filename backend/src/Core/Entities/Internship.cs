using GestionStagesMEN.Core.Enums;

namespace GestionStagesMEN.Core.Entities;

/// <summary>
/// Stage en cours — créé automatiquement après la signature de la convention.
/// </summary>
public class Internship
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid AgreementId { get; set; }
    public InternshipAgreement Agreement { get; set; } = null!;

    public string Sujet { get; set; } = string.Empty;
    public string? DescriptionDetaillee { get; set; }

    public DateOnly DateDebutEffective { get; set; }
    public DateOnly? DateFinEffective { get; set; }

    public InternshipStatus Statut { get; set; } = InternshipStatus.EnAttente;
    public bool IsArchived { get; set; } = false;
    public DateTime? DemarreAt { get; set; }
    public DateTime? TermineAt { get; set; }

    public Guid? SupervisorId { get; set; }
    public Supervisor? Supervisor { get; set; }

    // Navigation
    public List<InternshipTask> Taches { get; set; } = new();
    public List<InternshipEvaluation> Evaluations { get; set; } = new();
    public List<InternshipReport> Rapports { get; set; } = new();

    // ── Méthodes métier ──

    public void Demarrer()
    {
        Statut = InternshipStatus.EnCours;
        DemarreAt = DateTime.UtcNow;
    }

    public void Terminer()
    {
        DateFinEffective = DateOnly.FromDateTime(DateTime.UtcNow);
        Statut = InternshipStatus.Termine;
        TermineAt = DateTime.UtcNow;
    }
}
