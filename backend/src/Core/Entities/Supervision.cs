namespace GestionStagesMEN.Core.Entities;

/// <summary>
/// Relation Encadrant ↔ Étudiant (supervision active).
/// </summary>
public class Supervision
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid SupervisorId { get; set; }
    public Supervisor Supervisor { get; set; } = null!;

    public Guid StudentId { get; set; }
    public Student Student { get; set; } = null!;

    public DateTime AssigneAt { get; set; } = DateTime.UtcNow;
    public DateTime? FinAt { get; set; }
}
