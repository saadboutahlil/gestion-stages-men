namespace GestionStagesMEN.Core.Entities;

/// <summary>
/// Direction/Service du Ministère (ex: DSI, DRH, DSSP).
/// Remplace l'entité Company du projet précédent.
/// </summary>
public class Direction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? UserId { get; set; }                   // Responsable RH de la direction
    public ApplicationUser? User { get; set; }

    public string Nom { get; set; } = string.Empty;     // ex: Direction des Systèmes d'Information
    public string Sigle { get; set; } = string.Empty;   // ex: DSI
    public string? Adresse { get; set; }
    public string? Telephone { get; set; }
    public string? Email { get; set; }

    // Navigation
    public List<InternshipOffer> Offers { get; set; } = new();
    public List<Supervisor> Encadrants { get; set; } = new();
}
