namespace GestionStagesMEN.Core.Entities;

/// <summary>
/// Encadrant (tuteur terrain) — lié à une Direction et optionnellement à un compte utilisateur.
/// </summary>
public class Supervisor
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    public Guid DirectionId { get; set; }
    public Direction Direction { get; set; } = null!;

    public string NomComplet { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Telephone { get; set; }
    public string? Fonction { get; set; }        // ex: Ingénieur Développement, Chef de Projet
    public string? Service { get; set; }          // ex: Service Développement

    // Navigation
    public List<Supervision> Supervisions { get; set; } = new();
    public List<InternshipEvaluation> Evaluations { get; set; } = new();
}
