using GestionStagesMEN.Core.Enums;

namespace GestionStagesMEN.Core.Entities;

/// <summary>
/// Tâche assignée à un stagiaire par son encadrant.
/// </summary>
public class InternshipTask
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid InternshipId { get; set; }
    public Internship Internship { get; set; } = null!;

    public string Titre { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? DatePrevue { get; set; }
    public DateTime? DateCompletion { get; set; }
    public TaskItemStatus Statut { get; set; } = TaskItemStatus.AFaire;

    public void Demarrer() => Statut = TaskItemStatus.EnCours;

    public void Terminer()
    {
        Statut = TaskItemStatus.Terminee;
        DateCompletion = DateTime.UtcNow;
    }
}
