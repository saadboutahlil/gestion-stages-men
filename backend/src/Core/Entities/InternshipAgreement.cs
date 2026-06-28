using GestionStagesMEN.Core.Enums;

namespace GestionStagesMEN.Core.Entities;

/// <summary>
/// Convention de stage — document tripartite (Étudiant, Ministère, École).
/// </summary>
public class InternshipAgreement
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ApplicationId { get; set; }
    public InternshipApplication Application { get; set; } = null!;

    // Période
    public DateOnly DateDebut { get; set; }
    public DateOnly DateFin { get; set; }

    // Conditions
    public decimal? GratificationMensuelle { get; set; }
    public string? Missions { get; set; }
    public string? Objectifs { get; set; }

    // Partie École
    public string? NumeroEtudiant { get; set; }
    public string? AnneeEtude { get; set; }
    public string? Parcours { get; set; }
    public string? ObjectifsPedagogiques { get; set; }
    public string? CadreApprentissage { get; set; }
    public int? NombreVisites { get; set; }
    public string? LivrablesAttendus { get; set; }
    public string? CriteresEvaluation { get; set; }

    // Partie Ministère (RH)
    public string? MissionsConcretes { get; set; }
    public string? NomTuteur { get; set; }
    public string? FonctionTuteur { get; set; }
    public string? EmailTuteur { get; set; }
    public string? TelephoneTuteur { get; set; }
    public string? HorairesTravail { get; set; }
    public bool? TeletravailPossible { get; set; }
    public string? MoyensFournis { get; set; }
    public string? GrilleEvaluation { get; set; }

    // Signatures
    public AgreementStatus Statut { get; set; } = AgreementStatus.Brouillon;
    public DateTime? SignatureEtudiantAt { get; set; }
    public DateTime? SignatureRHAt { get; set; }
    public DateTime? SignatureEcoleAt { get; set; }
    public string? PdfPath { get; set; }

    // Navigation
    public Internship? Internship { get; set; }

    // ── Méthodes métier ──

    public void SignerParEtudiant()
    {
        SignatureEtudiantAt = DateTime.UtcNow;
        Statut = AgreementStatus.AttenteSignatureRH;
    }

    public void SignerParRH()
    {
        if (SignatureEtudiantAt == null) return;
        SignatureRHAt = DateTime.UtcNow;
        Statut = AgreementStatus.AttenteSignatureEcole;
    }

    public void SignerParEcole()
    {
        if (SignatureRHAt == null) return;
        SignatureEcoleAt = DateTime.UtcNow;
        Statut = AgreementStatus.Signee;
    }
}
