using GestionStagesMEN.Core.Enums;

namespace GestionStagesMEN.Core.Entities;

/// <summary>
/// Évaluation d'un stagiaire par son encadrant (mi-parcours ou finale).
/// </summary>
public class InternshipEvaluation
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid InternshipId { get; set; }
    public Internship Internship { get; set; } = null!;

    public Guid EvaluateurId { get; set; }
    public Supervisor Evaluateur { get; set; } = null!;

    public EvaluationType Type { get; set; }
    public DateTime DateEvaluation { get; set; } = DateTime.UtcNow;

    // Notes sur 20
    public int? NoteTechnique { get; set; }
    public int? NoteComportement { get; set; }
    public int? NoteAutonomie { get; set; }
    public int? NoteGlobale { get; set; }

    // Commentaires
    public string? PointsForts { get; set; }
    public string? PointsAmeliorer { get; set; }
    public string? Recommandations { get; set; }
}
