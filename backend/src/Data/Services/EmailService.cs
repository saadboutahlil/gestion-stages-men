using System.Net;
using System.Net.Mail;
using GestionStagesMEN.Core.Interfaces;
using Microsoft.Extensions.Configuration;

namespace GestionStagesMEN.Data.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config;
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        var host = _config["Smtp:Host"];
        if (string.IsNullOrEmpty(host))
        {
            throw new InvalidOperationException("Le serveur SMTP n'est pas configuré dans appsettings.json. Impossible d'envoyer l'email.");
        }

        var port = int.Parse(_config["Smtp:Port"] ?? "587");
        var username = _config["Smtp:Username"];
        var password = _config["Smtp:Password"];
        var enableSsl = bool.Parse(_config["Smtp:EnableSsl"] ?? "true");
        var fromEmail = _config["Smtp:FromEmail"];
        var fromName = _config["Smtp:FromName"] ?? "Gestion Stages MEN";

        using var client = new SmtpClient(host, port)
        {
            Credentials = new NetworkCredential(username, password),
            EnableSsl = enableSsl
        };

        var mailMessage = new MailMessage
        {
            From = new MailAddress(fromEmail, fromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };

        mailMessage.To.Add(to);

        try
        {
            await client.SendMailAsync(mailMessage);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors de l'envoi de l'email : {ex.Message}");
        }
    }
}
