using GestionStagesMEN.Core.Entities;
using GestionStagesMEN.Core.Enums;
using GestionStagesMEN.Data;
using GestionStagesMEN.Data.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace GestionStagesMEN.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InternshipsController : ControllerBase
{
    private readonly AppDbContext _ctx;
    private readonly PdfService _pdf;
    private readonly GestionStagesMEN.Core.Interfaces.IEmailService _email;
    
    public InternshipsController(AppDbContext ctx, PdfService pdf, GestionStagesMEN.Core.Interfaces.IEmailService email)
    {
        _ctx = ctx;
        _pdf = pdf;
        _email = email;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

    /// <summary>POST /api/internships/start/{agreementId} — Démarrer un stage</summary>
    [HttpPost("start/{agreementId}")]
    [Authorize(Roles = "MinistereRH,Admin")]
    public async Task<IActionResult> Start(Guid agreementId)
    {
        var agreement = await _ctx.InternshipAgreements
            .Include(a => a.Application).ThenInclude(ap => ap.Offer)
            .FirstOrDefaultAsync(a => a.Id == agreementId);

        if (agreement == null) return NotFound();
        if (agreement.Statut != AgreementStatus.Signee)
            return BadRequest(new { error = "La convention doit être signée par les 3 parties." });

        var internship = new Internship
        {
            AgreementId = agreementId,
            Sujet = $"Stage — {agreement.Application.Offer.Titre}",
            DateDebutEffective = agreement.DateDebut,
            Statut = InternshipStatus.EnCours,
            DemarreAt = DateTime.UtcNow
        };

        agreement.Statut = AgreementStatus.Active;
        _ctx.Internships.Add(internship);
        await _ctx.SaveChangesAsync();

        return Ok(new { internship.Id, message = "Stage démarré avec succès !" });
    }

    /// <summary>GET /api/internships/my — Mon stage (Student)</summary>
    [HttpGet("my")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> MyInternship()
    {
        var userId = GetUserId();
        var student = await _ctx.Students.FirstOrDefaultAsync(s => s.UserId == userId);
        if (student == null) return Ok(new { });

        var internship = await _ctx.Internships
            .Include(i => i.Supervisor)
            .Include(i => i.Agreement).ThenInclude(a => a.Application).ThenInclude(ap => ap.Offer).ThenInclude(o => o.Direction)
            .Include(i => i.Taches)
            .Include(i => i.Rapports)
            .Include(i => i.Evaluations)
            .Where(i => i.Agreement.Application.StudentId == student.Id)
            .OrderByDescending(i => i.DemarreAt)
            .FirstOrDefaultAsync();

        if (internship == null) return Ok(new { });

        return Ok(MapInternship(internship));
    }

    /// <summary>GET /api/internships — Liste des stages (MinistereRH)</summary>
    [HttpGet]
    [Authorize(Roles = "MinistereRH,Admin")]
    public async Task<IActionResult> GetAll()
    {
        var internships = await _ctx.Internships
            .Include(i => i.Supervisor)
            .Include(i => i.Agreement).ThenInclude(a => a.Application).ThenInclude(ap => ap.Student).ThenInclude(s => s.User)
            .Include(i => i.Agreement).ThenInclude(a => a.Application).ThenInclude(ap => ap.Offer).ThenInclude(o => o.Direction)
            .Include(i => i.Taches)
            .Where(i => !i.IsArchived)
            .OrderByDescending(i => i.DemarreAt)
            .ToListAsync();

        return Ok(internships.Select(MapInternship));
    }
    /// <summary>GET /api/internships/archived — Liste des stages archivés (DSI)</summary>
    [HttpGet("archived")]
    [Authorize(Roles = "MinistereRH,Admin")]
    public async Task<IActionResult> GetArchived([FromQuery] int? year = null)
    {
        IQueryable<Internship> query = _ctx.Internships
            .Include(i => i.Supervisor)
            .Include(i => i.Agreement)
                .ThenInclude(a => a.Application)
                    .ThenInclude(ap => ap.Student)
                        .ThenInclude(s => s.User);

        if (year.HasValue)
        {
            query = query.Where(i => i.DateDebutEffective.Year == year.Value);
        }

        var internships = await query
            .OrderByDescending(i => i.DateDebutEffective)
            .ToListAsync();

        var result = internships.Select(i => {
            var student = i.Agreement?.Application?.Student;
            var user = student?.User;
            
            var subject = i.Sujet;
            if (string.IsNullOrWhiteSpace(subject) || 
                subject.Contains("non renseigné") || 
                subject.Contains("Stage non renseigné"))
            {
                subject = "Non renseigné";
            }

            var school = student?.Etablissement;
            if (string.IsNullOrWhiteSpace(school) || school.Contains("Inconnu"))
            {
                school = "Non renseigné";
            }

            var docsList = new List<string>();
            var missions = i.Agreement?.Missions;
            if (!string.IsNullOrWhiteSpace(missions) && !missions.Equals("Stage historique", StringComparison.OrdinalIgnoreCase))
            {
                docsList = missions.Split(new[] { '/', ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                                   .Select(d => d.Trim())
                                   .Where(d => !string.IsNullOrEmpty(d))
                                   .ToList();
            }

            var isConvSigned = i.IsArchived || (i.Agreement != null && 
                               (i.Agreement.Statut == AgreementStatus.Signee || 
                                i.Agreement.Statut == AgreementStatus.Active || 
                                i.Agreement.Statut == AgreementStatus.Terminee));

            return new
            {
                i.Id,
                Sujet = subject,
                DateDebut = i.DateDebutEffective,
                DateFin = i.DateFinEffective,
                Annee = i.DateDebutEffective.Year,
                FullName = user?.FullName ?? "Inconnu",
                Etablissement = school,
                Encadrant = i.Supervisor?.NomComplet ?? i.Agreement?.NomTuteur ?? "Non renseigné",
                Telephone = user?.PhoneNumber ?? "Non renseigné",
                Documents = docsList,
                IsArchived = i.IsArchived,
                ConventionSignee = isConvSigned,
                AgreementStatus = i.Agreement?.Statut.ToString()
            };
        });

        return Ok(result);
    }

    /// <summary>GET /api/internships/supervisor — Mes stagiaires (Encadrant)</summary>
    [HttpGet("supervisor")]
    [Authorize(Roles = "Encadrant")]
    public async Task<IActionResult> MyStagiaires()
    {
        var userId = GetUserId();
        var supervisor = await _ctx.Supervisors
            .Include(s => s.Supervisions).ThenInclude(sv => sv.Student).ThenInclude(st => st.User)
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (supervisor == null) return Ok(Array.Empty<object>());

        var internships = await _ctx.Internships
            .Include(i => i.Supervisor)
            .Include(i => i.Agreement).ThenInclude(a => a.Application).ThenInclude(ap => ap.Student).ThenInclude(s => s.User)
            .Include(i => i.Agreement).ThenInclude(a => a.Application).ThenInclude(ap => ap.Offer).ThenInclude(o => o.Direction)
            .Include(i => i.Taches)
            .Include(i => i.Rapports)
            .Include(i => i.Evaluations)
            .Where(i => i.SupervisorId == supervisor.Id && !i.IsArchived)
            .OrderByDescending(i => i.DemarreAt)
            .ToListAsync();

        return Ok(internships.Select(MapInternship));
    }

    /// <summary>GET /api/internships/school — Stages à suivre (School)</summary>
    [HttpGet("school")]
    [Authorize(Roles = "School")]
    public async Task<IActionResult> SchoolInternships()
    {
        var internships = await _ctx.Internships
            .Include(i => i.Supervisor)
            .Include(i => i.Agreement).ThenInclude(a => a.Application).ThenInclude(ap => ap.Student).ThenInclude(s => s.User)
            .Include(i => i.Agreement).ThenInclude(a => a.Application).ThenInclude(ap => ap.Offer).ThenInclude(o => o.Direction)
            .Include(i => i.Taches)
            .Where(i => !i.IsArchived)
            .OrderByDescending(i => i.DemarreAt)
            .ToListAsync();

        return Ok(internships.Select(MapInternship));
    }

    /// <summary>GET /api/internships/supervisors — Liste des encadrants disponibles</summary>
    [HttpGet("supervisors")]
    [Authorize(Roles = "MinistereRH,Admin")]
    public async Task<IActionResult> GetSupervisors()
    {
        var supervisors = await _ctx.Supervisors
            .Include(s => s.User)
            .OrderBy(s => s.NomComplet)
            .ToListAsync();

        return Ok(supervisors.Select(s => new { 
            s.Id, 
            FullName = s.NomComplet, 
            Email = s.User?.Email ?? "N/A",
            Department = s.Service,
            Fonction = s.Fonction,
            ActiveInternsCount = _ctx.Internships.Count(i => i.SupervisorId == s.Id && i.Statut == InternshipStatus.EnCours)
        }));
    }

    /// <summary>PUT /api/internships/{id}/assign-supervisor — Affecter un encadrant</summary>
    [HttpPut("{id}/assign-supervisor")]
    [Authorize(Roles = "MinistereRH,Admin")]
    public async Task<IActionResult> AssignSupervisor(Guid id, [FromBody] Guid supervisorId)
    {
        var internship = await _ctx.Internships.FindAsync(id);
        if (internship == null) return NotFound();

        internship.SupervisorId = supervisorId;
        await _ctx.SaveChangesAsync();
        return Ok(new { message = "Encadrant affecté avec succès." });
    }

    /// <summary>PUT /api/internships/{id}/complete — Terminer un stage (Encadrant)</summary>
    [HttpPut("{id}/complete")]
    [Authorize(Roles = "Encadrant,MinistereRH")]
    public async Task<IActionResult> Complete(Guid id)
    {
        var internship = await _ctx.Internships
            .Include(i => i.Agreement).ThenInclude(a => a.Application).ThenInclude(ap => ap.Student).ThenInclude(s => s.User)
            .FirstOrDefaultAsync(i => i.Id == id);
            
        if (internship == null) return NotFound();

        // Sécurité : Vérifier le rapport final approuvé
        var hasFinalApproved = await _ctx.InternshipReports
            .AnyAsync(r => r.InternshipId == id && r.Type == ReportType.Final && r.Statut == ReportStatus.Approuve);

        if (!hasFinalApproved)
            return BadRequest(new { error = "Impossible de clôturer le stage : le rapport final doit d'abord être déposé par l'étudiant et validé par le Ministère RH." });

        internship.Terminer();
        await _ctx.SaveChangesAsync();

        var studentEmail = internship.Agreement?.Application?.Student?.User?.Email;
        if (studentEmail != null)
        {
            await _email.SendEmailAsync(
                studentEmail,
                "Stage Clôturé & Attestation",
                "<p>Bonjour,</p><p>Votre stage est désormais officiellement clôturé. Votre attestation de stage est disponible en téléchargement depuis votre espace personnel.</p>"
            );
        }

        return Ok(new { message = "Stage terminé avec succès." });
    }

    /// <summary>GET /api/internships/{id}/attestation — Télécharger l'attestation PDF</summary>
    [HttpGet("{id}/attestation")]
    public async Task<IActionResult> DownloadAttestation(Guid id, [FromQuery] string nom = "M. [Nom du Directeur]", [FromQuery] string fonction = "Directeur des Ressources Humaines")
    {
        var internship = await _ctx.Internships
            .Include(i => i.Supervisor)
            .Include(i => i.Agreement).ThenInclude(a => a.Application).ThenInclude(ap => ap.Student).ThenInclude(s => s.User)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (internship == null) return NotFound();

        if (internship.Statut != InternshipStatus.Termine && !internship.IsArchived)
            return BadRequest(new { error = "Le stage doit être terminé pour générer une attestation." });

        var pdf = _pdf.GenererAttestationPdf(internship, nom, fonction);
        return File(pdf, "application/pdf", $"Attestation-{internship.Id.ToString()[..8]}.pdf");
    }

    private static object MapInternship(Internship i) => new
    {
        i.Id, i.Sujet, i.DescriptionDetaillee,
        i.DateDebutEffective, i.DateFinEffective,
        Statut = i.Statut.ToString(),
        i.DemarreAt, i.TermineAt,
        i.SupervisorId,
        IsArchived = i.IsArchived,
        Encadrant = i.Supervisor?.NomComplet,
        Etudiant = i.Agreement?.Application?.Student?.User?.FullName,
        Offre = i.Agreement?.Application?.Offer?.Titre,
        Direction = i.Agreement?.Application?.Offer?.Direction?.Nom,
        TachesTotal = i.Taches.Count,
        TachesTerminees = i.Taches.Count(t => t.Statut == TaskItemStatus.Terminee),
        Progression = i.Taches.Count > 0
            ? (int)(i.Taches.Count(t => t.Statut == TaskItemStatus.Terminee) * 100.0 / i.Taches.Count)
            : 0,
        AuraRapportMiParcoursValide = i.Rapports.Any(r => r.Type == ReportType.MiParcours && r.Statut == ReportStatus.Approuve),
        AuraRapportFinalValide = i.Rapports.Any(r => r.Type == ReportType.Final && r.Statut == ReportStatus.Approuve),
        Taches = i.Taches.Select(t => new { t.Id, t.Titre, t.Description, Statut = t.Statut.ToString(), t.DatePrevue, t.DateCompletion }),
        Evaluations = i.Evaluations?.Select(e => new { e.Id, Type = e.Type.ToString(), e.NoteGlobale, e.DateEvaluation }),
        Rapports = i.Rapports?.Select(r => new { r.Id, Type = r.Type.ToString(), r.Titre, Statut = r.Statut.ToString(), r.DateDepot })
    };
}
