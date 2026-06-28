namespace GestionStagesMEN.Core.Enums;

// ── Statut d'une offre de stage ──
public enum OfferStatus
{
    Brouillon = 0,
    Ouverte = 10,
    Fermee = 20,
    Archivee = 30
}

// ── Statut d'une candidature ──
public enum ApplicationStatus
{
    Soumise = 10,
    EnRevue = 20,
    Acceptee = 30,
    Refusee = 40,
    Retiree = 50
}

// ── Statut d'une convention ──
public enum AgreementStatus
{
    Brouillon = 10,
    AttenteRemplissageRH = 15,
    AttenteSignatureEtudiant = 20,
    AttenteSignatureRH = 30,
    AttenteSignatureEcole = 40,
    Signee = 50,
    Active = 60,
    Terminee = 70,
    Annulee = 80
}

// ── Statut d'un stage ──
public enum InternshipStatus
{
    EnAttente = 10,
    EnCours = 20,
    Suspendu = 30,
    Termine = 40,
    Annule = 50
}

// ── Statut d'une tâche ──
public enum TaskItemStatus
{
    AFaire = 10,
    EnCours = 20,
    Terminee = 30,
    Bloquee = 40
}

// ── Type de rapport ──
public enum ReportType
{
    MiParcours = 10,
    Final = 20
}

// ── Statut d'un rapport ──
public enum ReportStatus
{
    EnAttente = 10,
    Approuve = 20,
    Rejete = 30,
    ModificationsDemandees = 40
}

// ── Type d'évaluation ──
public enum EvaluationType
{
    MiParcours = 10,
    Finale = 20
}
