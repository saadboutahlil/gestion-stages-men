using GestionStagesMEN.Core.Enums;

namespace GestionStagesMEN.Core.Entities;

/// <summary>
/// Offre de stage publiée par une Direction du Ministère.
/// </summary>
public class InternshipOffer
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid DirectionId { get; set; }
    public Direction Direction { get; set; } = null!;

    public string Titre { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Competences { get; set; }               // Compétences requises
    public DateOnly DateDebut { get; set; }
    public DateOnly DateFin { get; set; }
    public decimal? GratificationMensuelle { get; set; }   // en MAD
    public int NombrePostes { get; set; } = 1;
    public string Lieu { get; set; } = "Rabat";

    public OfferStatus Statut { get; set; } = OfferStatus.Ouverte;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public List<InternshipApplication> Applications { get; set; } = new();
}
