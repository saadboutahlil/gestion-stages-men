using GestionStagesMEN.Core.Enums;

namespace GestionStagesMEN.Core.Entities;

/// <summary>
/// Rapport de stage (mi-parcours ou final) déposé par le stagiaire.
/// </summary>
public class InternshipReport
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid InternshipId { get; set; }
    public Internship Internship { get; set; } = null!;

    public ReportType Type { get; set; }
    public string Titre { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string CheminFichier { get; set; } = string.Empty;
    public string NomFichier { get; set; } = string.Empty;
    public long TailleFichier { get; set; }

    public ReportStatus Statut { get; set; } = ReportStatus.EnAttente;
    public DateTime DateDepot { get; set; } = DateTime.UtcNow;
    public DateTime? DateRevue { get; set; }
    public string? CommentaireReviseur { get; set; }
    public Guid? ReviseurId { get; set; }

    public void Approuver(Guid reviseurId, string? commentaire = null)
    {
        Statut = ReportStatus.Approuve;
        DateRevue = DateTime.UtcNow;
        ReviseurId = reviseurId;
        CommentaireReviseur = commentaire;
    }

    public void Rejeter(Guid reviseurId, string commentaire)
    {
        Statut = ReportStatus.Rejete;
        DateRevue = DateTime.UtcNow;
        ReviseurId = reviseurId;
        CommentaireReviseur = commentaire;
    }
}
