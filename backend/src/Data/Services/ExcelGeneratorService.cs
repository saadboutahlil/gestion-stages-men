using ClosedXML.Excel;
using GestionStagesMEN.Core.Entities;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace GestionStagesMEN.Data.Services;

public class ExcelGeneratorService
{
    private readonly AppDbContext _context;

    public ExcelGeneratorService(AppDbContext context)
    {
        _context = context;
    }

    public byte[] GenerateTestDataExcel()
    {
        using var workbook = new XLWorkbook();
        
        // --- Onglet 1 : Structure ---
        var structureSheet = workbook.Worksheets.Add("Structure");
        structureSheet.Cell(1, 1).Value = "Table";
        structureSheet.Cell(1, 2).Value = "Champ";
        structureSheet.Cell(1, 3).Value = "Type";
        structureSheet.Cell(1, 4).Value = "Nullable";
        structureSheet.Cell(1, 5).Value = "Description";
        
        var headerRow = structureSheet.Row(1);
        headerRow.Style.Font.Bold = true;
        headerRow.Style.Fill.BackgroundColor = XLColor.LightBlue;

        int row = 2;
        var entityTypes = _context.Model.GetEntityTypes();

        foreach (var entityType in entityTypes)
        {
            var tableName = entityType.GetTableName();
            foreach (var property in entityType.GetProperties())
            {
                structureSheet.Cell(row, 1).Value = tableName;
                structureSheet.Cell(row, 2).Value = property.Name;
                structureSheet.Cell(row, 3).Value = property.ClrType.Name;
                structureSheet.Cell(row, 4).Value = property.IsNullable ? "Oui" : "Non";
                structureSheet.Cell(row, 5).Value = GetFriendlyDescription(tableName ?? "Inconnue", property.Name);
                row++;
            }
        }
        structureSheet.Columns().AdjustToContents();

        // --- Onglet 2 : Données de test ---
        var dataSheet = workbook.Worksheets.Add("Données de test");
        int dataRow = 1;

        // Liste manuelle des tables à inclure dans les données de test (plus stable que la Reflection)
        dataRow = AddTableData<Student>(dataSheet, dataRow, "Students");
        dataRow = AddTableData<Direction>(dataSheet, dataRow, "Directions");
        dataRow = AddTableData<InternshipOffer>(dataSheet, dataRow, "InternshipOffers");
        dataRow = AddTableData<InternshipApplication>(dataSheet, dataRow, "InternshipApplications");
        dataRow = AddTableData<InternshipAgreement>(dataSheet, dataRow, "InternshipAgreements");
        dataRow = AddTableData<Internship>(dataSheet, dataRow, "Internships");
        dataRow = AddTableData<InternshipTask>(dataSheet, dataRow, "InternshipTasks");
        dataRow = AddTableData<InternshipReport>(dataSheet, dataRow, "InternshipReports");
        dataRow = AddTableData<InternshipEvaluation>(dataSheet, dataRow, "InternshipEvaluations");
        dataRow = AddTableData<Supervisor>(dataSheet, dataRow, "Supervisors");
        dataRow = AddTableData<School>(dataSheet, dataRow, "Schools");
        dataRow = AddTableData<AuditLog>(dataSheet, dataRow, "AuditLogs");
        
        dataSheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public string GetFriendlyDescription(string tableName, string propertyName)
    {
        return tableName switch
        {
            "Directions" => propertyName switch
            {
                "Nom" => "Nom complet de la direction ou du service (ex: Direction des Systèmes d'Information).",
                "Sigle" => "Abréviation ou acronyme de la direction (ex: DSI).",
                "Adresse" => "Adresse physique du local de la direction.",
                "Email" => "Adresse email de contact générique du service.",
                "Telephone" => "Numéro de téléphone du secrétariat ou du service.",
                "UserId" => "Identifiant du responsable RH rattaché à cette direction.",
                _ => $"Information {propertyName} de la table Directions."
            },
            "Internships" => propertyName switch
            {
                "Sujet" => "Thématique principale ou intitulé du projet de stage.",
                "DescriptionDetaillee" => "Résumé des missions et objectifs attendus durant le stage.",
                "Statut" => "État d'avancement (En cours, Terminé, Annulé, etc.).",
                "DateDebutEffective" => "Date réelle du début du stage au sein du ministère.",
                "DateFinEffective" => "Date réelle de fin de stage constatée.",
                "DemarreAt" => "Horodatage précis du démarrage administratif.",
                "TermineAt" => "Horodatage précis de la clôture du stage.",
                "SupervisorId" => "Identifiant de l'encadrant technique assigné.",
                _ => $"Champ technique {propertyName} pour le suivi du stage."
            },
            "InternshipAgreements" => propertyName switch
            {
                "AnneeEtude" => "Niveau d'étude actuel de l'étudiant (ex: 3ème année).",
                "GratificationMensuelle" => "Montant de l'indemnité mensuelle versée (en MAD).",
                "Statut" => "État de validation de la convention (Brouillon, Signée, etc.).",
                "DateDebut" => "Date de début prévue selon la convention.",
                "DateFin" => "Date de fin prévue selon la convention.",
                "Missions" => "Description sommaire des missions confiées.",
                "Objectifs" => "Résultats attendus à l'issue de la période.",
                "NomTuteur" => "Nom de la personne ressource au sein du ministère.",
                "SignatureEtudiantAt" => "Date à laquelle l'étudiant a signé numériquement.",
                "SignatureRHAt" => "Date de signature par la Direction des Ressources Humaines.",
                "SignatureEcoleAt" => "Date de validation finale par l'établissement scolaire.",
                _ => $"Donnée contractuelle : {propertyName}."
            },
            "InternshipOffers" => propertyName switch
            {
                "Titre" => "Intitulé de l'offre publiée.",
                "Description" => "Contenu détaillé de la proposition de stage.",
                "Competences" => "Liste des savoir-faire et outils requis.",
                "NombrePostes" => "Nombre maximal de candidats pouvant être retenus.",
                "Lieu" => "Ville ou site géographique du stage.",
                "Statut" => "Disponibilité de l'offre (Ouverte, Pourvue, Expirée).",
                _ => $"Caractéristique de l'offre : {propertyName}."
            },
            "Students" => propertyName switch
            {
                "CNE" => "Code National de l'Étudiant (identifiant unique académique).",
                "Filiere" => "Domaine de spécialisation ou parcours de formation.",
                "Promotion" => "Année de diplomation prévue.",
                "Etablissement" => "Nom de l'école ou de l'université d'origine.",
                _ => $"Profil étudiant : {propertyName}."
            },
            "InternshipReports" => propertyName switch
            {
                "Titre" => "Nom donné au rapport déposé.",
                "Statut" => "État de revue (En attente, Approuvé, Rejeté).",
                "DateDepot" => "Date à laquelle le fichier a été téléchargé.",
                "CommentaireReviseur" => "Remarques laissées par l'encadrant après lecture.",
                _ => $"Information de rapportage : {propertyName}."
            },
            "InternshipTasks" => propertyName switch
            {
                "Titre" => "Intitulé de la tâche ou de l'objectif hebdomadaire.",
                "Statut" => "Progression de la tâche (À faire, En cours, Terminée).",
                "DatePrevue" => "Échéance fixée pour la réalisation.",
                _ => $"Suivi d'activité : {propertyName}."
            },
            "InternshipEvaluations" => propertyName switch
            {
                "Type" => "Type d'évaluation (Mi-parcours ou Finale).",
                "NoteGlobale" => "Appréciation générale synthétisée.",
                "PointsForts" => "Qualités observées chez le stagiaire.",
                "PointsAmeliorer" => "Axes de progression identifiés.",
                _ => $"Donnée d'évaluation : {propertyName}."
            },
            _ => propertyName switch
            {
                "Id" => "Identifiant unique universel (GUID) de l'enregistrement.",
                "CreatedAt" => "Date et heure de création dans le système.",
                "UpdatedAt" => "Date de la dernière modification enregistrée.",
                _ => $"Donnée système : {propertyName}."
            }
        };
    }

    private int AddTableData<T>(IXLWorksheet sheet, int row, string tableName) where T : class
    {
        sheet.Cell(row, 1).Value = $"Table : {tableName}";
        sheet.Cell(row, 1).Style.Font.Bold = true;
        sheet.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        row++;

        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                 .Where(p => p.PropertyType.IsPrimitive || p.PropertyType == typeof(string) || p.PropertyType == typeof(DateTime) || p.PropertyType == typeof(Guid) || p.PropertyType == typeof(decimal) || p.PropertyType == typeof(DateOnly))
                                 .ToList();

        // Headers
        for (int i = 0; i < properties.Count; i++)
        {
            sheet.Cell(row, i + 1).Value = properties[i].Name;
            sheet.Cell(row, i + 1).Style.Font.Bold = true;
        }
        row++;

        // Fetch 5 rows or use mock data if DB is empty
        try 
        {
            var data = _context.Set<T>().Take(5).ToList();
            
            if (data.Count == 0)
            {
                // Add some placeholder rows if empty
                for (int r = 0; r < 2; r++)
                {
                    for (int i = 0; i < properties.Count; i++)
                    {
                        sheet.Cell(row, i + 1).Value = "Exemple " + (r + 1);
                    }
                    row++;
                }
            }
            else
            {
                foreach (var item in data)
                {
                    for (int i = 0; i < properties.Count; i++)
                    {
                        var val = properties[i].GetValue(item);
                        sheet.Cell(row, i + 1).Value = val?.ToString() ?? "";
                    }
                    row++;
                }
            }
        }
        catch 
        {
            sheet.Cell(row, 1).Value = "Impossible de charger les données pour cette table.";
            row++;
        }
        
        row += 2; // Spacer
        return row;
    }
}
