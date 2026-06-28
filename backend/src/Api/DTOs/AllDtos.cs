namespace GestionStagesMEN.Api.DTOs;

// ══════════════════════════════════════
// AUTH
// ══════════════════════════════════════
public record LoginDto(string Email, string Password);
public record RegisterDto(string Email, string Password, string FullName, string Role);
public record RegisterStudentDto(string Email, string Password, string FullName, string CNE, string Filiere, string Promotion, string? Etablissement);

public record UnifiedRegisterDto(
    string Email, 
    string Password, 
    string FullName, 
    string Role,
    // Étudiant
    string? CNE, string? Filiere, string? Promotion, string? Etablissement,
    // RH
    string? Direction,
    // Encadrant
    string? Fonction, string? Telephone,
    // School
    string? NomEtablissement, string? Adresse, string? TelEtablissement
);

public record UserInfoDto(Guid Id, string Email, string FullName, string Role, bool IsActive);
public record LoginResponseDto(string Token);

// ══════════════════════════════════════
// OFFRES
// ══════════════════════════════════════
public record CreateOfferDto(string Titre, string Description, string? Competences, DateOnly DateDebut, DateOnly DateFin, decimal? GratificationMensuelle, int NombrePostes, string Lieu);
public record UpdateOfferDto(string Titre, string Description, string? Competences, DateOnly DateDebut, DateOnly DateFin, decimal? GratificationMensuelle, int NombrePostes, string Lieu);

// ══════════════════════════════════════
// CANDIDATURES
// ══════════════════════════════════════
public record ApplyDto(Guid OfferId, string? Message);

// ══════════════════════════════════════
// CONVENTIONS
// ══════════════════════════════════════
public record CreateAgreementDto(
    DateOnly DateDebut, DateOnly DateFin,
    string? NumeroEtudiant, string? AnneeEtude, string? Parcours,
    string? ObjectifsPedagogiques, string? CadreApprentissage,
    int? NombreVisites, string? LivrablesAttendus, string? CriteresEvaluation);

public record FillAgreementRHDto(
    string? MissionsConcretes, string? NomTuteur, string? FonctionTuteur,
    string? EmailTuteur, string? TelephoneTuteur, decimal? GratificationMensuelle,
    string? HorairesTravail, bool? TeletravailPossible, string? MoyensFournis, string? GrilleEvaluation);

// ══════════════════════════════════════
// TÂCHES
// ══════════════════════════════════════
public record CreateTaskDto(Guid InternshipId, string Titre, string? Description, DateTime? DatePrevue);

// ══════════════════════════════════════
// RAPPORTS
// ══════════════════════════════════════
public record ReviewReportDto(string? Commentaire);

// ══════════════════════════════════════
// ÉVALUATIONS
// ══════════════════════════════════════
public record CreateEvaluationDto(
    Guid InternshipId, string Type,
    int? NoteTechnique, int? NoteComportement, int? NoteAutonomie, int? NoteGlobale,
    string? PointsForts, string? PointsAmeliorer, string? Recommandations);

// ══════════════════════════════════════
// ADMIN
// ══════════════════════════════════════
public record AdminCreateUserDto(string Email, string Password, string FullName, string Role);
public record AdminUpdateUserDto(string FullName, string? Role, bool IsActive);
public record UpdateSettingDto(string Valeur);
