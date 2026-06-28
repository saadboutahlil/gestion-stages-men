namespace GestionStagesMEN.Core.Entities;

/// <summary>
/// Log d'audit — trace toutes les actions importantes dans l'application.
/// </summary>
public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? UserId { get; set; }
    public string? UserName { get; set; }
    public string Action { get; set; } = string.Empty;       // ex: "LOGIN", "CREATE_OFFER", "SIGN_AGREEMENT"
    public string? EntityType { get; set; }                    // ex: "InternshipOffer", "InternshipAgreement"
    public string? EntityId { get; set; }
    public string? Details { get; set; }                       // JSON libre
    public string? IpAddress { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
