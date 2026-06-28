using GestionStagesMEN.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionStagesMEN.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SearchController : ControllerBase
{
    private readonly AppDbContext _ctx;

    public SearchController(AppDbContext ctx)
    {
        _ctx = ctx;
    }

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return Ok(new { internships = new object[]{}, agreements = new object[]{}, users = new object[]{}, offers = new object[]{} });
        }

        var term = q.ToLower();

        // 1. Internships
        var internships = await _ctx.Internships
            .Include(i => i.Agreement).ThenInclude(a => a.Application).ThenInclude(ap => ap.Student).ThenInclude(s => s.User)
            .Where(i => i.Sujet.ToLower().Contains(term) 
                     || (i.Agreement.Application.Student.User.FullName != null && i.Agreement.Application.Student.User.FullName.ToLower().Contains(term)))
            .Select(i => new {
                i.Id,
                i.Sujet,
                Etudiant = i.Agreement.Application.Student.User.FullName,
                DateDebut = i.DateDebutEffective,
                DateFin = i.DateFinEffective,
                Statut = i.Statut.ToString()
            })
            .Take(10)
            .ToListAsync();

        // 2. Agreements
        var agreements = await _ctx.InternshipAgreements
            .Include(a => a.Application).ThenInclude(ap => ap.Student).ThenInclude(s => s.User)
            .Where(a => (a.NumeroEtudiant != null && a.NumeroEtudiant.ToLower().Contains(term))
                     || (a.Application.Student.User.FullName != null && a.Application.Student.User.FullName.ToLower().Contains(term))
                     || a.Statut.ToString().ToLower().Contains(term))
            .Select(a => new {
                a.Id,
                a.NumeroEtudiant,
                Etudiant = a.Application.Student.User.FullName,
                DateDebut = a.DateDebut,
                DateFin = a.DateFin,
                Statut = a.Statut.ToString()
            })
            .Take(10)
            .ToListAsync();

        // 3. Users
        var users = await _ctx.Users
            .Where(u => u.FullName.ToLower().Contains(term) || u.Email.ToLower().Contains(term))
            .Select(u => new {
                u.Id,
                u.FullName,
                u.Email,
                Role = _ctx.UserRoles.Where(ur => ur.UserId == u.Id).Join(_ctx.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name).FirstOrDefault()
            })
            .Take(10)
            .ToListAsync();

        // 4. Offers
        var offers = await _ctx.InternshipOffers
            .Where(o => o.Titre.ToLower().Contains(term) || o.Description.ToLower().Contains(term))
            .Select(o => new {
                o.Id,
                o.Titre,
                DateDebut = o.DateDebut,
                DateFin = o.DateFin,
                Statut = o.Statut.ToString()
            })
            .Take(10)
            .ToListAsync();

        return Ok(new
        {
            internships,
            agreements,
            users,
            offers
        });
    }
}
