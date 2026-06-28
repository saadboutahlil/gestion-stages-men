using GestionStagesMEN.Api.DTOs;
using GestionStagesMEN.Core.Entities;
using GestionStagesMEN.Core.Enums;
using GestionStagesMEN.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace GestionStagesMEN.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OffersController : ControllerBase
{
    private readonly AppDbContext _ctx;
    public OffersController(AppDbContext ctx) => _ctx = ctx;

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier) ?? User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)!);

    /// <summary>GET /api/offers — Liste des offres (filtrée par statut)</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? statut)
    {
        var query = _ctx.InternshipOffers
            .Include(o => o.Direction)
            .Include(o => o.Applications)
            .AsQueryable();

        if (!string.IsNullOrEmpty(statut) && Enum.TryParse<OfferStatus>(statut, true, out var s))
            query = query.Where(o => o.Statut == s);
        else
            query = query.Where(o => o.Statut == OfferStatus.Ouverte);

        var offers = await query.OrderByDescending(o => o.CreatedAt).ToListAsync();

        return Ok(offers.Select(o => new
        {
            o.Id, o.Titre, o.Description, o.Competences,
            o.DateDebut, o.DateFin, o.GratificationMensuelle,
            o.NombrePostes, o.Lieu, Statut = o.Statut.ToString(),
            Direction = o.Direction.Nom,
            DirectionSigle = o.Direction.Sigle,
            NombreCandidatures = o.Applications.Count,
            o.CreatedAt
        }));
    }

    /// <summary>GET /api/offers/{id}</summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var o = await _ctx.InternshipOffers
            .Include(x => x.Direction)
            .Include(x => x.Applications).ThenInclude(a => a.Student).ThenInclude(s => s.User)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (o == null) return NotFound();

        return Ok(new
        {
            o.Id, o.Titre, o.Description, o.Competences,
            o.DateDebut, o.DateFin, o.GratificationMensuelle,
            o.NombrePostes, o.Lieu, Statut = o.Statut.ToString(),
            Direction = o.Direction.Nom,
            DirectionSigle = o.Direction.Sigle,
            NombreCandidatures = o.Applications.Count,
            o.CreatedAt,
            Candidatures = o.Applications.Select(a => new
            {
                a.Id, Statut = a.Statut.ToString(),
                Etudiant = a.Student.User.FullName,
                a.DatePostulation, a.Message
            })
        });
    }

    /// <summary>POST /api/offers — Créer une offre (MinistereRH)</summary>
    [HttpPost]
    [Authorize(Roles = "MinistereRH,Admin")]
    public async Task<IActionResult> Create([FromBody] CreateOfferDto dto)
    {
        var userId = GetUserId();
        var direction = await _ctx.Directions.FirstOrDefaultAsync(d => d.UserId == userId);
        
        // Si aucune direction spécifique n'est trouvée, on continuera avec la direction par défaut plus bas

        // Si pas de direction liée, on prend la première direction par défaut
        if (direction == null) direction = await _ctx.Directions.FirstOrDefaultAsync();
        
        if (direction == null) return BadRequest(new { error = "Aucune direction configurée dans le système." });

        var offer = new InternshipOffer
        {
            DirectionId = direction.Id,
            Titre = dto.Titre,
            Description = dto.Description,
            Competences = dto.Competences,
            DateDebut = dto.DateDebut,
            DateFin = dto.DateFin,
            GratificationMensuelle = dto.GratificationMensuelle,
            NombrePostes = dto.NombrePostes,
            Lieu = dto.Lieu,
            Statut = OfferStatus.Ouverte
        };

        _ctx.InternshipOffers.Add(offer);
        await _ctx.SaveChangesAsync();
        return Ok(new { offer.Id, message = "Offre créée avec succès." });
    }

    /// <summary>PUT /api/offers/{id} — Modifier une offre</summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "MinistereRH,Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateOfferDto dto)
    {
        var offer = await _ctx.InternshipOffers.FindAsync(id);
        if (offer == null) return NotFound();

        offer.Titre = dto.Titre;
        offer.Description = dto.Description;
        offer.Competences = dto.Competences;
        offer.DateDebut = dto.DateDebut;
        offer.DateFin = dto.DateFin;
        offer.GratificationMensuelle = dto.GratificationMensuelle;
        offer.NombrePostes = dto.NombrePostes;
        offer.Lieu = dto.Lieu;

        await _ctx.SaveChangesAsync();
        return Ok(new { message = "Offre mise à jour." });
    }

    /// <summary>PUT /api/offers/{id}/close — Fermer une offre</summary>
    [HttpPut("{id}/close")]
    [Authorize(Roles = "MinistereRH,Admin")]
    public async Task<IActionResult> Close(Guid id)
    {
        var offer = await _ctx.InternshipOffers.FindAsync(id);
        if (offer == null) return NotFound();

        offer.Statut = OfferStatus.Fermee;
        await _ctx.SaveChangesAsync();
        return Ok(new { message = "Offre fermée." });
    }

    /// <summary>DELETE /api/offers/{id}</summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "MinistereRH,Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var offer = await _ctx.InternshipOffers.FindAsync(id);
        if (offer == null) return NotFound();

        _ctx.InternshipOffers.Remove(offer);
        await _ctx.SaveChangesAsync();
        return Ok(new { message = "Offre supprimée." });
    }
}
