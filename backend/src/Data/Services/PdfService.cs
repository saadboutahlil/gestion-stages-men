using GestionStagesMEN.Core.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GestionStagesMEN.Data.Services;

/// <summary>
/// Génère les PDF des conventions et attestations de stage.
/// </summary>
public class PdfService
{
    private static readonly string NavyBlue = "#1e3a5f";
    private static readonly string LightBlue = "#4a90e2";
    private static readonly string LightGrey = "#f5f5f5";

    public PdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    private static void ConfigureHeader(PageDescriptor page, string title, string address, string? refNumber = null)
    {
        page.Header().Column(col =>
        {
            col.Item().PaddingBottom(0.5f, Unit.Centimetre).BorderBottom(2).BorderColor(NavyBlue).PaddingBottom(5).Row(row =>
            {
                row.RelativeItem().Column(innerCol =>
                {
                    innerCol.Item().Text("ROYAUME DU MAROC").FontSize(10).FontColor(NavyBlue).Bold();
                    innerCol.Item().Text("Ministère de l'Éducation Nationale").FontSize(9).FontColor(Colors.Grey.Darken2);
                    innerCol.Item().Text("Direction des Systèmes d'Information (DSI)").FontSize(9).FontColor(Colors.Grey.Darken2);
                    innerCol.Item().Text(address).FontSize(8).FontColor(Colors.Grey.Darken2).Italic();
                });
                if (!string.IsNullOrEmpty(refNumber))
                {
                    row.RelativeItem().AlignRight().AlignBottom().Text($"Réf: {refNumber}").FontSize(9).FontColor(Colors.Grey.Darken2).Italic();
                }
            });

            col.Item().PaddingTop(1, Unit.Centimetre).AlignCenter().Text(title)
                .FontSize(22).FontColor(NavyBlue).Bold();
        });
    }

    private static void ConfigureFooter(PageDescriptor page, string documentType)
    {
        page.Footer().Column(col =>
        {
            col.Item().LineHorizontal(1).LineColor(LightGrey);
            col.Item().PaddingTop(5).AlignCenter().Text(x =>
            {
                x.Span($"Ministère de l'Éducation Nationale — {documentType} — Généré le ").FontSize(8).FontColor(Colors.Grey.Darken2);
                x.Span($"{DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8).FontColor(Colors.Grey.Darken2).Bold();
            });
            col.Item().AlignCenter().Text(x =>
            {
                x.Span("Page ").FontSize(8).FontColor(Colors.Grey.Darken2);
                x.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Darken2);
                x.Span(" sur ").FontSize(8).FontColor(Colors.Grey.Darken2);
                x.TotalPages().FontSize(8).FontColor(Colors.Grey.Darken2);
            });
        });
    }

    public byte[] GenererConventionPdf(InternshipAgreement convention, InternshipApplication candidature)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontFamily("Helvetica").FontSize(11).FontColor(Colors.Black));

                // Address condition logic
                var isOld = convention.SignatureEtudiantAt.HasValue && convention.SignatureEtudiantAt.Value < new DateTime(2026, 5, 22);
                var address = isOld ? "Bab Rouah, Rabat, Maroc" : "Avenue Ibn Rochd, Rabat, Maroc";

                // Header
                ConfigureHeader(page, "CONVENTION DE STAGE", address, convention.Id.ToString()[..8].ToUpper());

                // Content
                page.Content().PaddingVertical(1, Unit.Centimetre).Column(column =>
                {
                    // Parties
                    Section(column, "ARTICLE 1 — LES PARTIES");
                    column.Item().Background(LightGrey).Padding(10).Column(sub =>
                    {
                        var dir = candidature.Offer?.Direction;
                        sub.Item().Text(x => { x.Span("Direction d'accueil : ").Bold(); x.Span($"{dir?.Nom ?? "N/A"} ({dir?.Sigle ?? ""})"); });
                        sub.Item().Text(x => { x.Span("Adresse : ").Bold(); x.Span(dir?.Adresse ?? "Rabat"); });
                        sub.Item().PaddingTop(10).Text(x => { x.Span("Stagiaire : ").Bold(); x.Span(candidature.Student?.User?.FullName ?? "N/A"); });
                        sub.Item().Text(x => { x.Span("N° étudiant : ").Bold(); x.Span(convention.NumeroEtudiant ?? "N/A"); });
                        sub.Item().Text(x => { x.Span("Formation : ").Bold(); x.Span($"{convention.AnneeEtude ?? ""} — {convention.Parcours ?? ""}"); });
                    });

                    // Objet
                    Section(column, "ARTICLE 2 — OBJET DU STAGE");
                    Info(column, "Intitulé", candidature.Offer?.Titre ?? "N/A");
                    if (!string.IsNullOrEmpty(convention.MissionsConcretes))
                        Info(column, "Missions", convention.MissionsConcretes);

                    // Période
                    Section(column, "ARTICLE 3 — DURÉE ET PÉRIODE");
                    Info(column, "Période", $"Du {convention.DateDebut:dd/MM/yyyy} au {convention.DateFin:dd/MM/yyyy}");

                    // Conditions
                    Section(column, "ARTICLE 4 — CONDITIONS PRATIQUES");
                    Info(column, "Gratification", convention.GratificationMensuelle.HasValue ? $"{convention.GratificationMensuelle} MAD/mois" : "Non rémunéré");
                    Info(column, "Horaires", convention.HorairesTravail ?? "À définir");
                    Info(column, "Télétravail", convention.TeletravailPossible == true ? "Autorisé" : "Non autorisé");
                    Info(column, "Moyens", convention.MoyensFournis ?? "À définir");

                    // Tuteur
                    if (!string.IsNullOrEmpty(convention.NomTuteur))
                    {
                        Section(column, "ARTICLE 5 — ENCADREMENT");
                        Info(column, "Tuteur", $"{convention.NomTuteur} ({convention.FonctionTuteur ?? ""})");
                        Info(column, "Contact", $"{convention.EmailTuteur ?? ""} | {convention.TelephoneTuteur ?? ""}");
                    }

                    // Objectifs
                    if (!string.IsNullOrEmpty(convention.ObjectifsPedagogiques))
                    {
                        Section(column, "ARTICLE 6 — OBJECTIFS PÉDAGOGIQUES");
                        Info(column, "Compétences visées", convention.ObjectifsPedagogiques);
                        Info(column, "Livrables attendus", convention.LivrablesAttendus ?? "N/A");
                    }

                    // Signatures
                    column.Item().PaddingTop(1, Unit.Centimetre).Column(sigCol =>
                    {
                        sigCol.Item().AlignCenter().Text("SIGNATURES DES PARTIES").FontSize(12).FontColor(NavyBlue).Bold();
                        sigCol.Item().PaddingTop(15).Row(row =>
                        {
                            SignatureBox(row, "LE STAGIAIRE", convention.SignatureEtudiantAt);
                            SignatureBox(row, "LE MINISTÈRE", convention.SignatureRHAt);
                            SignatureBox(row, "L'ÉTABLISSEMENT", convention.SignatureEcoleAt);
                        });
                    });
                });

                // Footer
                ConfigureFooter(page, "Convention de Stage");
            });
        });

        return document.GeneratePdf();
    }

    public byte[] GenererAttestationPdf(Internship internship, string nomSignataire, string fonctionSignataire)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontFamily("Helvetica").FontSize(12).FontColor(Colors.Black));

                // Header
                ConfigureHeader(page, "ATTESTATION DE STAGE", "Avenue Ibn Rochd, Rabat, Maroc");

                // Content
                page.Content().PaddingVertical(2, Unit.Centimetre).Column(column =>
                {
                    column.Spacing(15);

                    var fullName = internship.Agreement?.Application?.Student?.User?.FullName ?? "N/A";

                    column.Item().Text(x =>
                    {
                        x.Span("Je soussigné(e) ").FontSize(12);
                        x.Span(nomSignataire).Bold().FontSize(12);
                        x.Span(", ").FontSize(12);
                        x.Span(fonctionSignataire).Bold().FontSize(12);
                        x.Span(" au sein du Ministère de l'Éducation Nationale (DSI),").FontSize(12);
                    });

                    column.Item().PaddingTop(10).PaddingBottom(10).AlignCenter().Text("Atteste par la présente que :").FontSize(14).Bold().FontColor(NavyBlue);

                    column.Item().AlignCenter().Text(x =>
                    {
                        x.Span("M./Mme ").FontSize(14);
                        x.Span(fullName).FontSize(16).Bold().FontColor(LightBlue);
                    });

                    column.Item().PaddingTop(10).Text(x =>
                    {
                        x.Span("a effectué un stage pratique au sein de notre Direction des Systèmes d'Information du ");
                        x.Span($"{internship.DateDebutEffective:dd/MM/yyyy}").Bold();
                        x.Span(" au ");
                        x.Span($"{internship.DateFinEffective:dd/MM/yyyy}").Bold();
                        x.Span(".");
                    });

                    column.Item().Text(x =>
                    {
                        x.Span("Durant cette période, le stage a porté sur le sujet suivant : ");
                        x.Span($"\"{internship.Sujet}\"").Bold().Italic();
                        x.Span(".");
                    });

                    if (!string.IsNullOrEmpty(internship.DescriptionDetaillee))
                    {
                        column.Item().PaddingTop(10).Background(LightGrey).Padding(10).Text(x =>
                        {
                            x.Span("Appréciation générale : ").Bold().FontColor(NavyBlue);
                            x.Span(internship.DescriptionDetaillee);
                        });
                    }

                    column.Item().PaddingTop(30).Text("Cette attestation est délivrée à l'intéressé(e) pour servir et valoir ce que de droit.").Italic();

                    // Signature block
                    column.Item().PaddingTop(40).AlignRight().Column(sigCol =>
                    {
                        sigCol.Item().Text($"Fait à Rabat, le {DateTime.Now:dd/MM/yyyy}").FontSize(11);
                        sigCol.Item().PaddingTop(10).Text(fonctionSignataire).Bold().FontColor(NavyBlue);
                        sigCol.Item().PaddingTop(5).Text(nomSignataire).FontSize(11);
                    });
                });

                // Footer
                ConfigureFooter(page, "Attestation de Stage");
            });
        });

        return document.GeneratePdf();
    }

    private static void Section(ColumnDescriptor column, string titre)
    {
        column.Item().PaddingTop(20).PaddingBottom(5).Text(titre).FontSize(12).FontColor(NavyBlue).Bold();
    }

    private static void Info(ColumnDescriptor column, string label, string value)
    {
        column.Item().PaddingBottom(3).Row(row =>
        {
            row.ConstantItem(120).Text(label).Bold().FontColor(NavyBlue).FontSize(10);
            row.RelativeItem().Text(value).FontSize(10);
        });
    }

    private static void SignatureBox(RowDescriptor row, string label, DateTime? signedAt)
    {
        row.RelativeItem().PaddingHorizontal(5).Border(1).BorderColor(LightBlue).Padding(10).Column(col =>
        {
            col.Item().AlignCenter().Text(label).FontSize(10).FontColor(NavyBlue).Bold();
            col.Item().PaddingTop(25).AlignCenter()
                .Text(signedAt.HasValue ? $"Signé le {signedAt.Value:dd/MM/yyyy}" : "En attente")
                .FontSize(9).Italic().FontColor(signedAt.HasValue ? Colors.Green.Darken2 : Colors.Grey.Medium);
        });
    }
}
