using GestionStagesMEN.Api.DTOs;
using GestionStagesMEN.Core.Entities;
using GestionStagesMEN.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace GestionStagesMEN.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IConfiguration _config;
    private readonly AppDbContext _ctx;

    public AuthController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IConfiguration config, AppDbContext ctx)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _config = config;
        _ctx = ctx;
    }

    /// <summary>POST /api/auth/login</summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null || !user.IsActive)
            return Unauthorized(new { error = "Email ou mot de passe incorrect." });

        var valid = await _userManager.CheckPasswordAsync(user, dto.Password);
        if (!valid)
            return Unauthorized(new { error = "Email ou mot de passe incorrect." });

        var token = await GenerateJwtToken(user);

        // Log d'audit
        _ctx.AuditLogs.Add(new AuditLog
        {
            UserId = user.Id,
            UserName = user.FullName,
            Action = "LOGIN",
            Details = $"Connexion réussie depuis {HttpContext.Connection.RemoteIpAddress}",
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        });
        await _ctx.SaveChangesAsync();

        return Ok(new LoginResponseDto(token));
    }

    /// <summary>POST /api/auth/register — Inscription dynamique multi-rôles</summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] UnifiedRegisterDto dto)
    {
        // 1. Création de l'utilisateur de base
        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            FullName = dto.FullName,
            EmailConfirmed = true,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        // 2. Attribution du rôle
        await _userManager.AddToRoleAsync(user, dto.Role);

        // 3. Création du profil spécifique selon le rôle
        switch (dto.Role)
        {
            case "Student":
                _ctx.Students.Add(new Student
                {
                    UserId = user.Id,
                    CNE = dto.CNE ?? "",
                    Filiere = dto.Filiere ?? "",
                    Promotion = dto.Promotion ?? "",
                    Etablissement = dto.Etablissement ?? ""
                });
                break;

            case "MinistereRH":
                // On peut soit créer une Direction, soit lier à une existante.
                // Ici on crée une nouvelle Direction pour le RH.
                _ctx.Directions.Add(new Direction
                {
                    UserId = user.Id,
                    Nom = dto.Direction ?? $"Direction de {dto.FullName}",
                    Sigle = (dto.Direction ?? dto.FullName).Substring(0, Math.Min(5, (dto.Direction ?? dto.FullName).Length)).ToUpper(),
                    Email = dto.Email
                });
                break;

            case "Encadrant":
                // Pour l'encadrant, il faut une DirectionId. 
                // On va chercher la première direction par défaut ou une spécifique.
                var firstDir = await _ctx.Directions.FirstOrDefaultAsync();
                _ctx.Supervisors.Add(new Supervisor
                {
                    UserId = user.Id,
                    DirectionId = firstDir?.Id ?? Guid.Empty,
                    NomComplet = dto.FullName,
                    Email = dto.Email,
                    Fonction = dto.Fonction,
                    Telephone = dto.Telephone
                });
                break;

            case "School":
                _ctx.Schools.Add(new School
                {
                    UserId = user.Id,
                    NomEtablissement = dto.NomEtablissement ?? dto.FullName,
                    Adresse = dto.Adresse,
                    Telephone = dto.TelEtablissement,
                    EmailContact = dto.Email
                });
                break;

            case "Admin":
                // Pas de profil spécifique pour l'admin
                break;
        }

        await _ctx.SaveChangesAsync();

        // Log d'audit
        _ctx.AuditLogs.Add(new AuditLog
        {
            UserId = user.Id,
            UserName = user.FullName,
            Action = "REGISTER",
            Details = $"Nouvel utilisateur créé avec le rôle {dto.Role}",
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        });
        await _ctx.SaveChangesAsync();

        return Ok(new { message = "Compte créé avec succès." });
    }

    /// <summary>GET /api/auth/me — Infos de l'utilisateur connecté</summary>
    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var userId = GetUserId();
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return NotFound();

        var roles = await _userManager.GetRolesAsync(user);
        return Ok(new UserInfoDto(user.Id, user.Email!, user.FullName, roles.FirstOrDefault() ?? "", user.IsActive));
    }

    // ── Helpers ──

    private Guid GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.Parse(sub!);
    }

    private async Task<string> GenerateJwtToken(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new("fullName", user.FullName),
            new(ClaimTypes.Role, roles.FirstOrDefault() ?? "")
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>PUT /api/auth/profile — Mettre à jour son profil</summary>
    [HttpPut("profile")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return NotFound();

        if (!string.IsNullOrEmpty(dto.FullName)) user.FullName = dto.FullName;
        
        if (!string.IsNullOrEmpty(dto.Email) && dto.Email != user.Email)
        {
            var setEmailResult = await _userManager.SetEmailAsync(user, dto.Email);
            if (!setEmailResult.Succeeded) return BadRequest(new { error = "Erreur lors de la mise à jour de l'email." });
            await _userManager.SetUserNameAsync(user, dto.Email);
        }

        if (!string.IsNullOrEmpty(dto.NewPassword)) 
        {
            if (string.IsNullOrEmpty(dto.OldPassword)) 
                return BadRequest(new { error = "L'ancien mot de passe est requis pour le modifier." });
            
            var checkPwd = await _userManager.CheckPasswordAsync(user, dto.OldPassword);
            if (!checkPwd) 
                return BadRequest(new { error = "L'ancien mot de passe est incorrect." });

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetResult = await _userManager.ResetPasswordAsync(user, token, dto.NewPassword);
            if (!resetResult.Succeeded) 
                return BadRequest(new { error = "Erreur lors de la modification du mot de passe." });
        }

        await _userManager.UpdateAsync(user);
        return Ok(new { message = "Profil mis à jour avec succès." });
    }
}

public record UpdateProfileDto(string? FullName, string? Email, string? OldPassword, string? NewPassword);
