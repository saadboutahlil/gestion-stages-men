using GestionStagesMEN.Api.DTOs;
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
public class AgreementsController : ControllerBase
{
    private readonly AppDbContext _ctx;
    private readonly PdfService _pdf;
    private readonly GestionStagesMEN.Core.Interfaces.IEmailService _email;
    private readonly IConfiguration _config;
    public AgreementsController(AppDbContext ctx, PdfService pdf, GestionStagesMEN.Core.Interfaces.IEmailService email, IConfiguration config) 
    { 
        _ctx = ctx; 
        _pdf = pdf; 
        _email = email;
        _config = config;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

    /// <summary>GET /api/agreements — Toutes les conventions (MinistereRH)</summary>
    [HttpGet]
    [Authorize(Roles = "MinistereRH,Admin")]
    public async Task<IActionResult> GetAll()
    {
        var agreements = await _ctx.InternshipAgreements
            .Include(a => a.Application).ThenInclude(ap => ap.Student).ThenInclude(s => s.User)
            .Include(a => a.Application).ThenInclude(ap => ap.Offer).ThenInclude(o => o.Direction)
            .Where(a => a.Application.Offer.Statut != OfferStatus.Archivee)
            .OrderByDescending(a => a.DateDebut)
            .ToListAsync();

        return Ok(agreements.Select(MapAgreement));
    }

    /// <summary>GET /api/agreements/pending-school — Toutes les conventions pour l'école</summary>
    [HttpGet("pending-school")]
    [Authorize(Roles = "School")]
    public async Task<IActionResult> PendingSchool()
    {
        var agreements = await _ctx.InternshipAgreements
            .Include(a => a.Application).ThenInclude(ap => ap.Student).ThenInclude(s => s.User)
            .Include(a => a.Application).ThenInclude(ap => ap.Offer).ThenInclude(o => o.Direction)
            .Where(a => a.Application.Offer.Statut != OfferStatus.Archivee)
            .OrderByDescending(a => a.DateDebut)
            .ToListAsync();

        return Ok(agreements.Select(MapAgreement));
    }

    /// <summary>GET /api/agreements/my — Convention de l'étudiant connecté</summary>
    [HttpGet("my")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> MyAgreement()
    {
        var userId = GetUserId();
        var student = await _ctx.Students.FirstOrDefaultAsync(s => s.UserId == userId);
        if (student == null) return Ok(Array.Empty<object>());

        var agreements = await _ctx.InternshipAgreements
            .Include(a => a.Application).ThenInclude(ap => ap.Student).ThenInclude(s => s.User)
            .Include(a => a.Application).ThenInclude(ap => ap.Offer).ThenInclude(o => o.Direction)
            .Where(a => a.Application.StudentId == student.Id)
            .ToListAsync();

        return Ok(agreements.Select(MapAgreement));
    }

    /// <summary>POST /api/agreements/create/{applicationId} — Créer convention (School)</summary>
    [HttpPost("create/{applicationId}")]
    [Authorize(Roles = "School")]
    public async Task<IActionResult> Create(Guid applicationId, [FromBody] CreateAgreementDto dto)
    {
        var app = await _ctx.InternshipApplications.FindAsync(applicationId);
        if (app == null) return NotFound();
        if (app.Statut != ApplicationStatus.Acceptee)
            return BadRequest(new { error = "La candidature doit être acceptée." });

        var agreement = new InternshipAgreement
        {
            ApplicationId = applicationId,
            DateDebut = dto.DateDebut,
            DateFin = dto.DateFin,
            NumeroEtudiant = dto.NumeroEtudiant,
            AnneeEtude = dto.AnneeEtude,
            Parcours = dto.Parcours,
            ObjectifsPedagogiques = dto.ObjectifsPedagogiques,
            CadreApprentissage = dto.CadreApprentissage,
            NombreVisites = dto.NombreVisites,
            LivrablesAttendus = dto.LivrablesAttendus,
            CriteresEvaluation = dto.CriteresEvaluation,
            Statut = AgreementStatus.AttenteRemplissageRH
        };

        _ctx.InternshipAgreements.Add(agreement);
        await _ctx.SaveChangesAsync();

        var rhEmail = _config["Smtp:RhEmail"];
        if (!string.IsNullOrEmpty(rhEmail))
        {
            await _email.SendEmailAsync(
                rhEmail,
                "Nouvelle Convention Créée",
                $"<p>Bonjour,</p><p>Une nouvelle convention de stage a été créée par un établissement (ID Candidature: {applicationId}). Merci de procéder au remplissage de la partie RH.</p>"
            );
        }

        return Ok(new { agreement.Id, message = "Convention créée — en attente du RH." });
    }

    /// <summary>PUT /api/agreements/{id}/fill-rh — Remplir partie RH</summary>
    [HttpPut("{id}/fill-rh")]
    [Authorize(Roles = "MinistereRH")]
    public async Task<IActionResult> FillRH(Guid id, [FromBody] FillAgreementRHDto dto)
    {
        var a = await _ctx.InternshipAgreements
            .Include(ag => ag.Application).ThenInclude(ap => ap.Student).ThenInclude(s => s.User)
            .FirstOrDefaultAsync(ag => ag.Id == id);
        if (a == null) return NotFound();

        a.MissionsConcretes = dto.MissionsConcretes;
        a.NomTuteur = dto.NomTuteur;
        a.FonctionTuteur = dto.FonctionTuteur;
        a.EmailTuteur = dto.EmailTuteur;
        a.TelephoneTuteur = dto.TelephoneTuteur;
        a.GratificationMensuelle = dto.GratificationMensuelle;
        a.HorairesTravail = dto.HorairesTravail;
        a.TeletravailPossible = dto.TeletravailPossible;
        a.MoyensFournis = dto.MoyensFournis;
        a.GrilleEvaluation = dto.GrilleEvaluation;
        a.Statut = AgreementStatus.AttenteSignatureEtudiant;

        await _ctx.SaveChangesAsync();

        if (a.Application?.Student?.User?.Email != null)
        {
            await _email.SendEmailAsync(
                a.Application.Student.User.Email,
                "Convention Prête pour Signature",
                "<p>Bonjour,</p><p>Votre convention de stage a été remplie par le Ministère. Veuillez vous connecter pour la signer électroniquement.</p>"
            );
        }

        return Ok(new { message = "Convention remplie — en attente signature étudiant." });
    }

    /// <summary>PUT /api/agreements/{id}/sign/student</summary>
    [HttpPut("{id}/sign/student")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> SignStudent(Guid id)
    {
        var a = await _ctx.InternshipAgreements.FindAsync(id);
        if (a == null) return NotFound();
        a.SignerParEtudiant();
        await _ctx.SaveChangesAsync();
        return Ok(new { message = "Convention signée par l'étudiant." });
    }

    /// <summary>PUT /api/agreements/{id}/sign/rh</summary>
    [HttpPut("{id}/sign/rh")]
    [Authorize(Roles = "MinistereRH")]
    public async Task<IActionResult> SignRH(Guid id)
    {
        var a = await _ctx.InternshipAgreements.FindAsync(id);
        if (a == null) return NotFound();
        a.SignerParRH();
        await _ctx.SaveChangesAsync();
        return Ok(new { message = "Convention signée par le Ministère." });
    }

    /// <summary>PUT /api/agreements/{id}/sign/school</summary>
    [HttpPut("{id}/sign/school")]
    [Authorize(Roles = "School")]
    public async Task<IActionResult> SignSchool(Guid id)
    {
        var a = await _ctx.InternshipAgreements
            .Include(ag => ag.Application).ThenInclude(app => app.Offer)
            .FirstOrDefaultAsync(ag => ag.Id == id);
            
        if (a == null) return NotFound();
        a.SignerParEcole();
        
        // Démarrage automatique du stage après la dernière signature (École)
        var internship = new Internship
        {
            AgreementId = a.Id,
            Sujet = a.Application.Offer?.Titre ?? "Stage",
            DateDebutEffective = DateOnly.FromDateTime(DateTime.UtcNow)
        };
        internship.Demarrer();
        _ctx.Internships.Add(internship);
        
        await _ctx.SaveChangesAsync();
        return Ok(new { message = "Convention signée par l'école. Stage démarré !" });
    }

    /// <summary>GET /api/agreements/{id}/pdf — Télécharger le PDF</summary>
    [HttpGet("{id}/pdf")]
    public async Task<IActionResult> DownloadPdf(Guid id)
    {
        var agreement = await _ctx.InternshipAgreements
            .Include(a => a.Application).ThenInclude(ap => ap.Student).ThenInclude(s => s.User)
            .Include(a => a.Application).ThenInclude(ap => ap.Offer).ThenInclude(o => o.Direction)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (agreement == null) return NotFound();

        if (agreement.Statut != AgreementStatus.Signee && agreement.Statut != AgreementStatus.Active)
            return BadRequest(new { error = "La convention n'est pas encore finalisée." });

        var pdf = _pdf.GenererConventionPdf(agreement, agreement.Application);
        return File(pdf, "application/pdf", $"Convention-{agreement.Id.ToString()[..8]}.pdf");
    }

    private static object MapAgreement(InternshipAgreement a) => new
    {
        a.Id,
        Statut = a.Statut.ToString(),
        Etudiant = a.Application?.Student?.User?.FullName,
        Offre = a.Application?.Offer?.Titre,
        Direction = a.Application?.Offer?.Direction?.Nom,
        a.DateDebut, a.DateFin,
        a.GratificationMensuelle,
        a.SignatureEtudiantAt, a.SignatureRHAt, a.SignatureEcoleAt,
        a.MissionsConcretes, a.NomTuteur,
        IsArchived = a.Application?.Offer?.Statut == OfferStatus.Archivee
    };
}
