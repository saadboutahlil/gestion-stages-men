using GestionStagesMEN.Data;
using GestionStagesMEN.Core.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GestionStagesMEN.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _ctx;
    public DashboardController(AppDbContext ctx) => _ctx = ctx;

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString)) return Unauthorized();
        
        var userId = Guid.Parse(userIdString);
        var role = User.FindFirstValue(ClaimTypes.Role);

        if (role == "Student")
        {
            var student = await _ctx.Students.FirstOrDefaultAsync(s => s.UserId == userId);
            if (student == null) return Ok(new { CandidaturesTotal = 0, CandidaturesAcceptees = 0 });

            var candidatures = await _ctx.InternshipApplications.Where(a => a.StudentId == student.Id).ToListAsync();
            var stage = await _ctx.Internships
                .Include(i => i.Taches)
                .Include(i => i.Rapports)
                .FirstOrDefaultAsync(i => i.Agreement.Application.StudentId == student.Id && i.Statut == InternshipStatus.EnCours);

            return Ok(new
            {
                CandidaturesTotal = candidatures.Count,
                CandidaturesAcceptees = candidatures.Count(a => a.Statut == ApplicationStatus.Acceptee),
                HasStage = stage != null,
                StageSujet = stage?.Sujet,
                Progression = stage != null && stage.Taches.Any() ? (stage.Taches.Count(t => t.Statut == TaskItemStatus.Terminee) * 100 / stage.Taches.Count) : 0,
                TachesTotal = stage?.Taches.Count ?? 0,
                TachesTerminees = stage?.Taches.Count(t => t.Statut == TaskItemStatus.Terminee) ?? 0,
                RapportsDeposes = stage?.Rapports.Count ?? 0
            });
        }

        if (role == "MinistereRH")
        {
            return Ok(new
            {
                OffresOuvertes = await _ctx.InternshipOffers.CountAsync(o => o.Statut == OfferStatus.Ouverte),
                CandidaturesEnAttente = await _ctx.InternshipApplications.CountAsync(a => a.Statut == ApplicationStatus.Soumise),
                StagesEnCours = await _ctx.Internships.CountAsync(i => i.Statut == InternshipStatus.EnCours),
                ConventionsSignees = await _ctx.InternshipAgreements.CountAsync(a => a.Statut == AgreementStatus.Signee)
            });
        }

        if (role == "Encadrant")
        {
            var supervisor = await _ctx.Supervisors.FirstOrDefaultAsync(s => s.UserId == userId);
            if (supervisor == null) return Ok(new { MesStagiairesCount = 0, TachesEnAttente = 0, EvaluationsAFaire = 0 });

            return Ok(new
            {
                MesStagiairesCount = await _ctx.Internships.CountAsync(i => i.SupervisorId == supervisor.Id && i.Statut == InternshipStatus.EnCours),
                TachesEnAttente = await _ctx.InternshipTasks.CountAsync(t => t.Internship.SupervisorId == supervisor.Id && t.Statut != TaskItemStatus.Terminee),
                EvaluationsAFaire = await _ctx.Internships.CountAsync(i => i.SupervisorId == supervisor.Id && i.Statut == InternshipStatus.EnCours)
            });
        }

        if (role == "Admin")
        {
            return Ok(new
            {
                StagesActifs = await _ctx.Internships.CountAsync(i => i.Statut == InternshipStatus.EnCours),
                ConventionsSignees = await _ctx.InternshipAgreements.CountAsync(a => a.Statut == AgreementStatus.Signee),
                RapportsEnAttente = await _ctx.InternshipReports.CountAsync(r => r.Statut == ReportStatus.EnAttente)
            });
        }

        if (role == "School")
        {
            return Ok(new
            {
                CandidaturesAcceptees = await _ctx.InternshipApplications.CountAsync(a => a.Statut == ApplicationStatus.Acceptee),
                ConventionsASigner = await _ctx.InternshipAgreements.CountAsync(a => a.Statut == AgreementStatus.AttenteSignatureEcole),
                StagesASuivre = await _ctx.Internships.CountAsync(i => i.Statut == InternshipStatus.EnCours)
            });
        }

        return Ok(new { Message = "Stats non disponibles pour ce rôle" });
    }
}
