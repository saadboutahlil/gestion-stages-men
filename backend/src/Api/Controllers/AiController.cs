using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using GestionStagesMEN.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace GestionStagesMEN.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AiController : ControllerBase
{
    private readonly AppDbContext _ctx;
    private readonly IWebHostEnvironment _env;
    private readonly IConfiguration _config;
    private readonly HttpClient _httpClient;

    public AiController(AppDbContext ctx, IWebHostEnvironment env, IConfiguration config, HttpClient httpClient)
    {
        _ctx = ctx;
        _env = env;
        _config = config;
        _httpClient = httpClient;
    }

    [HttpPost("match-cv/{applicationId}")]
    [Authorize(Roles = "MinistereRH,Admin")]
    public async Task<IActionResult> MatchCv(Guid applicationId)
    {
        try 
        {
            // 1. Get Application & Offer
            var app = await _ctx.InternshipApplications
                .Include(a => a.Offer)
                .FirstOrDefaultAsync(a => a.Id == applicationId);

            if (app == null) return NotFound(new { error = "Candidature introuvable." });
            if (string.IsNullOrEmpty(app.CvPath)) return BadRequest(new { error = "Aucun CV n'est attaché à cette candidature." });

            // 2. Read PDF File
            var fileName = Path.GetFileName(app.CvPath);
            var filePath = Path.Combine(_env.ContentRootPath, "Uploads", "Cv", fileName);

            if (!System.IO.File.Exists(filePath))
                return BadRequest(new { error = "Le fichier PDF du CV est introuvable sur le serveur." });

            byte[] pdfBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            string base64Pdf = Convert.ToBase64String(pdfBytes);

            // --- MOCK TOGGLE ---
            // Mettez à true pour contourner les erreurs 429 (Quota) de Google pendant vos tests/captures.
            // Mettez à false pour utiliser la vraie API Gemini en production (nécessite une carte bancaire ou un quota valide).
            bool useMock = true;

            if (useMock)
            {
                var mockJson = "{ \"score\": 88, \"justification\": \"Analyse approfondie de la candidature :\\n\\n✨ Points forts identifiés :\\n• Adéquation technique : Le candidat démontre une forte maîtrise des technologies clés (C# / .NET 8, Angular 19) appuyée par des projets académiques concrets.\\n• Compétences transverses : Bonne compréhension de la sécurité applicative (JWT, RBAC) et de l'architecture logicielle.\\n• Profil académique : Le cursus en Génie Informatique correspond parfaitement aux exigences du Ministère.\\n\\n⚠️ Axes de développement :\\n• Le CV manque légèrement de détails sur les pratiques de déploiement (DevOps, CI/CD) qui seraient un atout pour la mise en production du projet.\\n\\n📌 Conclusion de l'IA :\\nProfil hautement compatible avec l'offre de stage. Les compétences fondamentales sont présentes. Il est vivement recommandé de valider cette candidature pour l'étape d'entretien technique.\" }";
                var mockResult = JsonSerializer.Deserialize<JsonElement>(mockJson);
                return Ok(mockResult);
            }

            // 3. Prepare Gemini API Request
            var apiKey = _config["Gemini:ApiKey"];
            if (string.IsNullOrEmpty(apiKey) || apiKey == "VOTRE_CLE_ICI")
                return StatusCode(500, new { error = "Clé API Gemini non configurée dans appsettings.json." });

            var promptText = $@"
Tu es un assistant expert en ressources humaines. Ta tâche est d'analyser un CV (fourni en PDF) et de le comparer aux exigences d'une offre de stage.

Détails de l'offre de stage :
- Titre : {app.Offer?.Titre}
- Description : {app.Offer?.Description}
- Compétences requises : {app.Offer?.Competences}

Instructions importantes :
1. Analyse les compétences, la formation et l'expérience présentes dans le CV.
2. Compare-les avec la description et les exigences de l'offre de stage.
3. Attribue un score de compatibilité entre 0 et 100.
4. Rédige une justification TRÈS détaillée et structurée.
5. Tu DOIS répondre UNIQUEMENT avec un objet JSON valide.

Format JSON attendu :
{{
  ""score"": 85,
  ""justification"": ""Le candidat possède de solides compétences...""
}}";

            var payload = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new object[]
                        {
                            new { text = promptText },
                            new
                            {
                                inlineData = new
                                {
                                    mimeType = "application/pdf",
                                    data = base64Pdf
                                }
                            }
                        }
                    }
                },
                generationConfig = new
                {
                    responseMimeType = "application/json",
                    temperature = 0.0
                }
            };

            var jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash-lite:generateContent?key={apiKey}";

            var response = await _httpClient.PostAsync(url, content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[GEMINI ERROR] Status: {response.StatusCode}");
                Console.WriteLine($"[GEMINI ERROR] Content: {errorContent}");
                if ((int)response.StatusCode == 429)
                    return StatusCode(429, new { error = "Le service IA est temporairement indisponible (quota atteint).", details = errorContent });
                return StatusCode(500, new { error = "Erreur de l'API Gemini.", details = errorContent });
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseJson);
            var textResult = doc.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();

            if (textResult != null)
                return Ok(JsonSerializer.Deserialize<JsonElement>(textResult));

            return BadRequest(new { error = "Réponse vide de l'IA." });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[INTERNAL ERROR] {ex.Message}");
            return StatusCode(500, new { error = "Erreur interne.", details = ex.Message });
        }
    }

    [HttpPost("summarize-report/{reportId}")]
    [Authorize(Roles = "MinistereRH,Admin,Student,Encadrant")]
    public async Task<IActionResult> SummarizeReport(Guid reportId)
    {
        try 
        {
            // 1. Get Report
            var report = await _ctx.InternshipReports.FindAsync(reportId);

            if (report == null) return NotFound(new { error = "Rapport introuvable." });
            if (string.IsNullOrEmpty(report.CheminFichier)) return BadRequest(new { error = "Aucun fichier attaché à ce rapport." });

            // 2. Read PDF File
            var fileName = Path.GetFileName(report.CheminFichier);
            var filePath = Path.Combine(_env.ContentRootPath, "Uploads", "rapports", fileName);

            if (!System.IO.File.Exists(filePath))
                return BadRequest(new { error = "Le fichier PDF du rapport est introuvable sur le serveur." });

            byte[] pdfBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            string base64Pdf = Convert.ToBase64String(pdfBytes);

            // --- MOCK TOGGLE ---
            bool useMock = true;
            if (useMock)
            {
                var mockJson = "{ \"score\": 85, \"summary\": \"(Analyse IA Mock) Ce rapport présente avec une grande clarté le travail réalisé durant le stage. Les objectifs principaux ont été atteints avec succès. L'étudiant a démontré une bonne maîtrise des technologies utilisées et a fait preuve d'une belle autonomie sur les missions confiées. Le document est bien structuré, malgré quelques axes d'amélioration possibles sur la partie tests et déploiement. Globalement, le travail fourni est de très bonne qualité et justifie une validation sans réserve.\" }";
                var mockResult = JsonSerializer.Deserialize<JsonElement>(mockJson);
                return Ok(mockResult);
            }

            // 3. Prepare Gemini API Request
            var apiKey = _config["Gemini:ApiKey"];
            if (string.IsNullOrEmpty(apiKey) || apiKey == "VOTRE_CLE_ICI")
                return StatusCode(500, new { error = "Clé API Gemini non configurée dans appsettings.json." });

            var promptText = $@"
Tu es un professeur académique expert et un évaluateur technique d'une université d'informatique. Ta tâche est de lire ce rapport de stage (fourni en PDF) et d'en faire une analyse très poussée.

Titre du rapport: {report.Titre}
Description: {report.Description}

Instructions:
1. Attribue une note globale de complétude et de qualité sur 100 (le score). Évalue la rigueur, le respect des standards, et le contenu technique.
2. Rédige un résumé TRÈS DÉTAILLÉ et structuré (environ 15 à 20 phrases).
3. Ton résumé doit obligatoirement inclure :
   - Les objectifs principaux du stage.
   - Les missions, tâches et réalisations concrètes de l'étudiant.
   - Les technologies et méthodes utilisées.
   - Une critique constructive sur les apports personnels, les difficultés rencontrées et la qualité de la conclusion.
4. Tu DOIS répondre UNIQUEMENT avec un objet JSON valide, sans markdown, contenant une propriété 'score' (un nombre) et une propriété 'summary' (un texte long).

Format JSON attendu :
{{
  ""score"": 88,
  ""summary"": ""Ce rapport présente avec une grande clarté le développement d'une application web...""
}}";

            var payload = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new object[]
                        {
                            new { text = promptText },
                            new
                            {
                                inlineData = new
                                {
                                    mimeType = "application/pdf",
                                    data = base64Pdf
                                }
                            }
                        }
                    }
                },
                generationConfig = new
                {
                    responseMimeType = "application/json",
                    temperature = 0.0
                }
            };

            var jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash-lite:generateContent?key={apiKey}";

            var response = await _httpClient.PostAsync(url, content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[GEMINI ERROR] Status: {response.StatusCode}");
                Console.WriteLine($"[GEMINI ERROR] Content: {errorContent}");
                if ((int)response.StatusCode == 429)
                    return StatusCode(429, new { error = "Le service IA est temporairement indisponible (quota atteint). Veuillez réessayer dans quelques instants." });
                return StatusCode(500, new { error = "Erreur de l'API Gemini.", details = errorContent });
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            
            using var doc = JsonDocument.Parse(responseJson);
            var root = doc.RootElement;
            
            var textResult = root
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            if (textResult != null)
            {
                var aiResult = JsonSerializer.Deserialize<JsonElement>(textResult);
                return Ok(aiResult);
            }

            return BadRequest(new { error = "Réponse vide de l'IA." });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[INTERNAL ERROR] {ex.Message}\n{ex.StackTrace}");
            return StatusCode(500, new { error = "Erreur interne du serveur lors de l'analyse IA.", details = ex.Message });
        }
    }

    // ── SENTIMENT ANALYSIS ────────────────────────────────────────────────────
    [HttpPost("analyze-sentiment")]
    [Authorize(Roles = "MinistereRH,Admin,Encadrant")]
    public async Task<IActionResult> AnalyzeSentiment([FromBody] SentimentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
            return BadRequest(new { error = "Le texte à analyser ne peut pas être vide." });

        string studentContext = string.IsNullOrEmpty(request.StudentName) ? "" : $" pour l'étudiant(e) {request.StudentName}";
        string userName = !string.IsNullOrEmpty(request.SupervisorName) ? request.SupervisorName : (User.Identity?.Name ?? "Inconnu");

        var apiKey = _config["Gemini:ApiKey"];
        bool useMock = true;
        bool geminiConfigured = !useMock && !string.IsNullOrEmpty(apiKey) && apiKey != "VOTRE_CLE_ICI";

        if (geminiConfigured)
        {
            try
            {
                var prompt = $@"Tu es un expert en analyse psychologique des commentaires professionnels dans le cadre des stages.

Analyse le commentaire d'évaluation suivant et retourne UNIQUEMENT un objet JSON valide (sans markdown, sans texte avant ou après).

Commentaire à analyser :
""{request.Text}""

Règles d'analyse :
- sentiment : ""positif"" si le commentaire est encourageant et valorisant, ""négatif"" si critique, frustrant ou décourageant, ""neutre"" dans tous les autres cas.
- alerte : true UNIQUEMENT si tu détectes un conflit potentiel, une frustration forte, un comportement problématique, un harcèlement ou une situation préoccupante.
- explication : 2 à 3 phrases expliquant ton analyse et les raisons de l'alerte si applicable.

Format JSON attendu :
{{
  ""sentiment"": ""positif"",
  ""alerte"": false,
  ""explication"": ""Le commentaire exprime une satisfaction générale...""
}}";

                var payload = new
                {
                    contents = new[] { new { parts = new[] { new { text = prompt } } } },
                    generationConfig = new { responseMimeType = "application/json", temperature = 0.2 }
                };

                var jsonPayload = JsonSerializer.Serialize(payload);
                var httpContent = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash-lite:generateContent?key={apiKey}";
                var response = await _httpClient.PostAsync(url, httpContent);

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(responseJson);
                    var textResult = doc.RootElement
                        .GetProperty("candidates")[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text")
                        .GetString();

                    if (textResult != null)
                    {
                        var result = JsonSerializer.Deserialize<JsonElement>(textResult);
                        bool alerte = result.TryGetProperty("alerte", out var alereProp) && alereProp.GetBoolean();

                        // If alert detected → write AuditLog for Admin visibility
                        if (alerte)
                        {
                            _ctx.AuditLogs.Add(new GestionStagesMEN.Core.Entities.AuditLog
                            {
                                Action = "SENTIMENT_ALERT",
                                UserName = userName,
                                EntityType = "InternshipEvaluation",
                                Details = $"{{\"commentaire\": {JsonSerializer.Serialize(request.Text.Length > 200 ? request.Text[..200] + "..." : request.Text)}, \"explication\": {JsonSerializer.Serialize((result.TryGetProperty("explication", out var exp) ? exp.GetString() : "") + studentContext)}, \"source\": \"Gemini IA\"}}",
                                Timestamp = DateTime.UtcNow
                            });
                            await _ctx.SaveChangesAsync();
                        }

                        return Ok(result);
                    }
                }

                Console.WriteLine($"[SENTIMENT] Gemini indisponible (HTTP {(int)response.StatusCode}), bascule sur analyse locale.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SENTIMENT ERROR] {ex.Message}");
            }
        }

        // ── LOCAL FALLBACK (keyword-based) ────────────────────────────────────
        return Ok(GetLocalSentimentAnalysis(request.Text, userName, studentContext));
    }

    private object GetLocalSentimentAnalysis(string text, string userName, string studentContext)
    {
        var lower = text.ToLowerInvariant();

        var positiveWords = new[] { "excellent", "remarquable", "très bien", "bravo", "félicitations", "motivé", "sérieux", "efficace", "compétent", "bon travail", "progressé", "autonome", "initiative" };
        var negativeWords = new[] { "insuffisant", "mauvais", "problème", "conflit", "difficulté", "absent", "retard", "irresponsable", "manque", "déçu", "faible", "inacceptable", "comportement" };
        var alertWords = new[] { "conflit", "harcèlement", "agressif", "menace", "plainte", "situation grave", "inacceptable", "problème grave", "comportement inapproprié" };

        int positiveScore = positiveWords.Count(w => lower.Contains(w));
        int negativeScore = negativeWords.Count(w => lower.Contains(w));
        bool alerte = alertWords.Any(w => lower.Contains(w));

        string sentiment = positiveScore > negativeScore ? "positif"
                         : negativeScore > positiveScore ? "négatif"
                         : "neutre";

        string explication = sentiment == "positif"
            ? "Le commentaire contient des termes valorisants et encourageants. Aucun signal de tension détecté."
            : sentiment == "négatif"
            ? "Le commentaire contient des termes critiques ou préoccupants. Une attention particulière est recommandée."
            : "Le commentaire est factuel et mesuré. Pas de signal particulier détecté.";

        if (alerte) explication += " ⚠️ Des mots-clés sensibles ont été détectés — une vérification manuelle est conseillée.";

        if (alerte)
        {
            _ctx.AuditLogs.Add(new GestionStagesMEN.Core.Entities.AuditLog
            {
                Action = "SENTIMENT_ALERT",
                UserName = userName,
                EntityType = "InternshipEvaluation",
                Details = $"{{\"commentaire\": {JsonSerializer.Serialize(text.Length > 200 ? text[..200] + "..." : text)}, \"explication\": {JsonSerializer.Serialize(explication + studentContext)}, \"source\": \"Analyse locale\"}}",
                Timestamp = DateTime.UtcNow
            });
            _ctx.SaveChanges();
        }

        return new { sentiment, alerte, explication };
    }

    [HttpPost("chatbot")]
    [Authorize(Roles = "MinistereRH,Admin,Student,Encadrant,School")]
    public async Task<IActionResult> Chatbot([FromBody] ChatbotRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Question))
                return BadRequest(new { error = "La question ne peut pas être vide." });

            var apiKey = _config["Gemini:ApiKey"];
            bool useMock = true;
            bool geminiConfigured = !useMock && !string.IsNullOrEmpty(apiKey) && apiKey != "VOTRE_CLE_ICI";

            if (geminiConfigured)
            {
                string systemPrompt = @"Tu es l'assistant virtuel officiel de la plateforme 'Gestion des Stages MEN' (Ministère de l'Éducation Nationale du Maroc). Ton rôle est de répondre aux questions concernant le fonctionnement de la plateforme.

Voici le contexte métier et les règles de la plateforme :
1. WORKFLOW COMPLET : Offre de stage -> Candidature -> Acceptation RH -> Création de la Convention (École) -> Signatures (Tripartite) -> Stage Actif -> Assignation Encadrant -> Tâches & Évaluations -> Dépôt des Rapports -> Validation RH -> Attestation de fin de stage.
2. ORDRE DES SIGNATURES : L'étudiant signe en premier, puis le Ministère (RH), et enfin l'École. Le stage devient 'EnCours' uniquement après les 3 signatures.
3. RÔLES :
   - Student (Étudiant) : Postule aux offres (1 seule candidature à la fois), signe sa convention, visualise ses tâches, dépose ses rapports (Mi-Parcours et Final).
   - MinistereRH (RH) : Publie les offres, accepte les candidatures, signe les conventions, assigne les encadrants, valide les rapports.
   - School (École) : Visualise les candidatures acceptées de ses étudiants, génère et télécharge le brouillon de la convention, signe la convention en dernier.
   - Encadrant (Superviseur) : Gère ses stagiaires, leur assigne des tâches, effectue les évaluations (Mi-Parcours et Finale), lit les rapports des stagiaires et peut utiliser l'IA pour évaluer un rapport.
   - Admin : Gère la plateforme, les utilisateurs, consulte les logs et statistiques.
4. RÈGLES : Un étudiant ne peut postuler qu'à une seule offre à la fois. Un stage ne peut être clôturé que si le rapport final est validé.

IMPORTANT :
- Sois concis. Réponds en 2-3 phrases maximum.
- Si besoin, propose un lien vers la page concernée en utilisant le format markdown strict [Texte](/chemin). Les chemins valides sont : /offres, /student/internship, /school/internships, /student/reports, /student/tasks, /admin/dashboard.
- Réponds TOUJOURS de manière polie, professionnelle, claire et en français. Si on te pose une question hors sujet, refuse poliment en rappelant ton rôle.";

                if (!string.IsNullOrEmpty(request.Role))
                    systemPrompt += $"\n\nL'utilisateur actuel a le rôle : {request.Role}. Adapte ta réponse spécifiquement à ce rôle.";

                var contents = new List<object>();
                if (request.History == null || !request.History.Any())
                {
                    contents.Add(new { role = "user", parts = new[] { new { text = systemPrompt + "\n\n" + request.Question } } });
                }
                else
                {
                    bool isFirst = true;
                    foreach (var msg in request.History)
                    {
                        string text = msg.Text;
                        if (isFirst && msg.Role == "user") { text = systemPrompt + "\n\n" + text; isFirst = false; }
                        contents.Add(new { role = msg.Role == "model" ? "model" : "user", parts = new[] { new { text = text } } });
                    }
                    contents.Add(new { role = "user", parts = new[] { new { text = request.Question } } });
                }

                var payload = new { contents = contents, generationConfig = new { responseMimeType = "text/plain", temperature = 0.7 } };
                var jsonPayload = JsonSerializer.Serialize(payload);
                var httpContent = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash-lite:generateContent?key={apiKey}";
                var response = await _httpClient.PostAsync(url, httpContent);

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(responseJson);
                    var textResult = doc.RootElement
                        .GetProperty("candidates")[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text")
                        .GetString();
                    return Ok(new { reponse = textResult });
                }

                // Gemini failed — log and fall through to local FAQ engine
                var errBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[CHATBOT] Gemini indisponible (HTTP {(int)response.StatusCode}), bascule sur FAQ locale. Détails: {errBody}");
            }

            // ── LOCAL FAQ FALLBACK ─────────────────────────────────────────────
            return Ok(new { reponse = GetLocalFallbackResponse(request.Question.ToLowerInvariant(), request.Role ?? "") });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CHATBOT ERROR] {ex.Message}");
            // Even on crash, return a helpful local answer instead of an error
            return Ok(new { reponse = GetLocalFallbackResponse(request.Question.ToLowerInvariant(), request.Role ?? "") });
        }
    }

    private static string GetLocalFallbackResponse(string q, string role)
    {
        bool isRH = role == "MinistereRH";
        bool isSchool = role == "School";
        bool isEncadrant = role == "Encadrant";
        bool isAdmin = role == "Admin";
        bool isStudent = role == "Student";

        // ── SALUTATIONS ──────────────────────────────────────────────────────────
        if (q.Contains("bonjour") || q.Contains("salut") || q.Contains("hello") || q.Contains("bonsoir") || q.Contains("hi") || q.Contains("salam"))
        {
            if (isRH) return "Bonjour ! En tant que **Responsable RH**, vous pouvez publier des offres, gérer les candidatures, signer les conventions et valider les rapports. Comment puis-je vous aider ?";
            if (isSchool) return "Bonjour ! En tant qu'**École**, vous pouvez consulter les candidatures acceptées de vos étudiants, générer et signer les conventions. Comment puis-je vous aider ?";
            if (isEncadrant) return "Bonjour ! En tant qu'**Encadrant**, vous gérez vos stagiaires, assignez des tâches et réalisez les évaluations. Comment puis-je vous aider ?";
            if (isAdmin) return "Bonjour ! En tant qu'**Administrateur**, vous avez accès à la gestion complète de la plateforme. Comment puis-je vous aider ?";
            return "Bonjour ! Je suis l'assistant de la plateforme **Gestion des Stages MEN**. Comment puis-je vous aider ?";
        }

        // ── FAQ SPÉCIFIQUE RH ────────────────────────────────────────────────────
        if (isRH)
        {
            // Rôle RH / que fait le RH
            if (q.Contains("rh") || q.Contains("ministere") || q.Contains("ministère") || q.Contains("que fais") || q.Contains("mon rôle") || q.Contains("mes fonctions") || q.Contains("que puis-je") || q.Contains("que je peux"))
                return "En tant que **RH Ministère**, vous pouvez : publier des offres de stage, **accepter ou refuser** les candidatures, signer les conventions (2ème signataire), assigner les encadrants, valider les rapports et clôturer les stages.";

            if (q.Contains("publier") || q.Contains("créer une offre") || q.Contains("nouvelle offre") || q.Contains("ajouter une offre"))
                return "Pour publier une offre de stage, rendez-vous dans la section **Offres** et cliquez sur **Nouvelle offre**. Vous pouvez définir le titre, la description, les compétences requises et la direction d'accueil.";

            if (q.Contains("accept") || q.Contains("valider une candidature") || q.Contains("candidature"))
                return "Pour gérer les candidatures, rendez-vous dans **Candidatures Reçues**. Vous pouvez accepter ✓ ou refuser ✕ chaque dossier. Vous pouvez aussi utiliser l'**analyse IA (✨)** pour scorer la compatibilité du CV avec l'offre.";

            if (q.Contains("signer") || q.Contains("signature") || q.Contains("convention"))
                return "Vous signez la convention en **2ème position** (après l'étudiant). Rendez-vous dans la liste des stages pour trouver la convention à signer. Une fois signée, l'école peut signer en dernier.";

            if (q.Contains("valider") && (q.Contains("rapport") || q.Contains("rapports")))
                return "Pour valider les rapports, allez dans **Validation des Rapports**. Vous pouvez utiliser l'IA (✨ Résumer) pour générer une analyse du rapport, puis valider ou rejeter avec un commentaire.";

            if (q.Contains("assigner") || q.Contains("encadrant") || q.Contains("affecter"))
                return "Pour assigner un encadrant à un stagiaire, rendez-vous dans **Suivi des Stages**, trouvez le stage et cliquez sur **Affecter**. Vous pourrez sélectionner un encadrant dans la liste.";

            if (q.Contains("rapport") || q.Contains("validation"))
                return "Les rapports soumis par les étudiants sont accessibles via **Validation des Rapports**. Vous pouvez générer un résumé IA, puis valider ou rejeter chaque rapport.";
        }

        // ── FAQ SPÉCIFIQUE ÉCOLE ─────────────────────────────────────────────────
        if (isSchool)
        {
            if (q.Contains("école") || q.Contains("ecole") || q.Contains("que fais") || q.Contains("mon rôle") || q.Contains("mes fonctions") || q.Contains("que puis-je"))
                return "En tant qu'**École**, vous consultez les candidatures acceptées de vos étudiants, **générez le brouillon de la convention**, et **signez la convention en dernier** (3ème signataire) pour activer le stage.";

            if (q.Contains("convention") || q.Contains("brouillon") || q.Contains("générer") || q.Contains("signer"))
                return "Rendez-vous dans [Conventions](/school/internships) pour voir les conventions de vos étudiants. Téléchargez le **brouillon PDF** et signez la convention une fois l'étudiant et le RH ayant signé.";

            if (q.Contains("candidature") || q.Contains("mes étudiants") || q.Contains("stagiaires"))
                return "Vous pouvez consulter les candidatures **acceptées** de vos étudiants depuis [Conventions](/school/internships). Seules les candidatures validées par le RH y apparaissent.";
        }

        // ── FAQ SPÉCIFIQUE ENCADRANT ─────────────────────────────────────────────
        if (isEncadrant)
        {
            if (q.Contains("encadrant") || q.Contains("que fais") || q.Contains("mon rôle") || q.Contains("mes fonctions") || q.Contains("que puis-je"))
                return "En tant qu'**Encadrant**, vous gérez vos stagiaires assignés, leur créez des **tâches**, réalisez les **évaluations Mi-Parcours et Finale**, et consultez leurs rapports (avec résumé IA).";

            if (q.Contains("tâche") || q.Contains("tache") || q.Contains("mission") || q.Contains("ajouter une tâche"))
                return "Pour ajouter une tâche, allez dans **Gestion des Missions**, trouvez le stagiaire et cliquez sur **+ Tâche**. Définissez le titre, la description et l'échéance.";

            if (q.Contains("évaluation") || q.Contains("évaluer") || q.Contains("noter") || q.Contains("bilan"))
                return "Pour évaluer un stagiaire, allez dans **Bilans & Évaluations** et cliquez sur **Évaluer**. La note finale nécessite que le **rapport final soit validé** par le RH au préalable.";

            if (q.Contains("rapport") || q.Contains("lire") || q.Contains("voir les rapports"))
                return "Consultez les rapports de vos stagiaires dans l'onglet **Rapports** de votre tableau de bord. Vous pouvez utiliser l'IA (✨ IA) pour générer un résumé automatique de chaque rapport.";

            if (q.Contains("mes stagiaires") || q.Contains("stagiaires") || q.Contains("mes étudiants"))
                return "Tous vos stagiaires sont listés dans **Suivi des Stages**. Vous y voyez leur progression, leur statut et les tâches accomplies.";
        }

        // ── FAQ SPÉCIFIQUE ADMIN ─────────────────────────────────────────────────
        if (isAdmin)
        {
            if (q.Contains("admin") || q.Contains("que fais") || q.Contains("mon rôle") || q.Contains("mes fonctions") || q.Contains("que puis-je"))
                return "En tant qu'**Administrateur**, vous gérez les utilisateurs et leurs rôles, consultez les logs d'activité, accédez aux statistiques globales et supervisez toute la plateforme.";

            if (q.Contains("utilisateur") || q.Contains("user") || q.Contains("compte"))
                return "La gestion des utilisateurs est accessible depuis le [Dashboard Admin](/admin/dashboard). Vous pouvez créer, modifier ou désactiver des comptes et changer les rôles.";

            if (q.Contains("log") || q.Contains("historique") || q.Contains("activité"))
                return "Les logs d'activité sont consultables depuis le **Dashboard Admin**. Ils enregistrent toutes les actions importantes effectuées sur la plateforme.";

            if (q.Contains("statistique") || q.Contains("rapport global") || q.Contains("tableau de bord"))
                return "Le [Dashboard Admin](/admin/dashboard) présente les statistiques globales : nombre de stages actifs, candidatures en cours, rapports en attente de validation, etc.";
        }

        // ── FAQ GÉNÉRALE (toutes rôles) ─────────────────────────────────────────

        // "Que fait le RH ?" demandé par un non-RH
        if (q.Contains("rh") || q.Contains("ministere") || q.Contains("ministère"))
            return "Le **RH Ministère** publie les offres de stage, accepte/refuse les candidatures, signe les conventions (2ème), assigne les encadrants et valide les rapports finaux.";

        // "Que fait l'école ?" demandé par un non-École
        if (q.Contains("école") || q.Contains("ecole"))
            return "L'**École** consulte les candidatures acceptées de ses étudiants, génère le brouillon de convention et signe en dernier (3ème signataire) pour activer le stage.";

        // Postuler / candidature
        if (q.Contains("postul") || q.Contains("candidat") || q.Contains("inscrire") || q.Contains("rejoindre") || q.Contains("appliquer"))
            return "Pour postuler, rendez-vous sur la page [Offres de stage](/offres), sélectionnez une offre et cliquez sur **Postuler**. Attention : vous ne pouvez soumettre qu'**une seule candidature à la fois**.";

        // Offres
        if (q.Contains("offre") || q.Contains("stage disponible") || q.Contains("trouver un stage") || q.Contains("voir les stages"))
            return "Toutes les offres de stage disponibles sont accessibles sur la page [Offres](/offres). Vous pouvez les consulter et postuler directement.";

        // Convention / signature
        if (q.Contains("convention") || q.Contains("signer") || q.Contains("signature") || q.Contains("contrat") || q.Contains("accord"))
            return "La convention est signée dans l'ordre : **1) Étudiant**, **2) RH Ministère**, **3) École**. Le stage ne devient actif qu'après les **3 signatures**.";

        // Rapport
        if (q.Contains("rapport") || q.Contains("déposer") || q.Contains("soumettre") || q.Contains("mi-parcours") || q.Contains("document"))
            return "Les rapports (Mi-Parcours et Final) sont déposés par l'étudiant depuis [Mes Rapports](/student/reports). Le rapport final doit être **validé par le RH** avant la clôture du stage.";

        // Tâches
        if (q.Contains("tâche") || q.Contains("tache") || q.Contains("mission") || q.Contains("travail") || q.Contains("todo"))
            return "Les tâches sont assignées par l'encadrant et visibles pour l'étudiant sur [Mes Tâches](/student/tasks). L'encadrant peut en ajouter, modifier ou supprimer à tout moment.";

        // Encadrant
        if (q.Contains("encadrant") || q.Contains("superviseur") || q.Contains("responsable") || q.Contains("tuteur"))
            return "L'**Encadrant** est assigné par le RH après l'acceptation de la candidature. Il gère les tâches et réalise les évaluations Mi-Parcours et Finale du stagiaire.";

        // Évaluation
        if (q.Contains("évaluation") || q.Contains("evaluat") || q.Contains("note") || q.Contains("notation") || q.Contains("score"))
            return "Les évaluations (Mi-Parcours et Finale) sont réalisées par l'**Encadrant**. La note finale nécessite que le rapport final soit validé par le RH.";

        // Statut du stage
        if (q.Contains("statut") || q.Contains("état") || q.Contains("suivi") || q.Contains("avancement") || q.Contains("progress"))
            return "Un stage devient **EnCours** après les 3 signatures de la convention. Il se clôture après la validation du rapport final par le RH.";

        // Rôles
        if (q.Contains("rôle") || q.Contains("role") || q.Contains("accès") || q.Contains("permission") || q.Contains("qui peut") || q.Contains("droits"))
            return "La plateforme compte 5 rôles : **Étudiant**, **MinistereRH**, **École**, **Encadrant** et **Admin**. Chaque rôle a des accès spécifiques dans le processus de stage.";

        // Admin / dashboard
        if (q.Contains("admin") || q.Contains("tableau de bord") || q.Contains("dashboard") || q.Contains("statistique") || q.Contains("log"))
            return "Le tableau de bord administrateur est accessible via [Dashboard](/admin/dashboard). Il présente les statistiques globales et les logs d'activité.";

        // Attestation
        if (q.Contains("attestation") || q.Contains("fin de stage") || q.Contains("certif") || q.Contains("terminé") || q.Contains("clôture"))
            return "L'attestation est générée automatiquement après la **validation du rapport final** par le RH. L'étudiant peut la télécharger depuis [Mon Stage](/student/internship).";

        // Workflow complet
        if (q.Contains("processus") || q.Contains("étapes") || q.Contains("comment ça marche") || q.Contains("workflow") || q.Contains("fonctionnement") || q.Contains("fonctionne") || q.Contains("utiliser"))
            return "Voici le fonctionnement complet de la plateforme, étape par étape :\n\n" +
                   "1. **Candidature :** Le RH publie une offre. L'étudiant postule en ligne avec son CV.\n" +
                   "2. **Validation :** Le RH accepte la candidature et assigne un encadrant.\n" +
                   "3. **Convention (Tripartite) :** L'école génère le brouillon. L'étudiant signe en premier, le RH en deuxième, et l'école en dernier pour activer officiellement le stage.\n" +
                   "4. **Déroulement :** L'étudiant suit ses tâches sur son tableau de bord. L'encadrant l'évalue (avec analyse de sentiment IA).\n" +
                   "5. **Livrables :** L'étudiant dépose ses rapports. L'IA peut générer un résumé instantané pour les évaluateurs.\n" +
                   "6. **Clôture :** Une fois le rapport final validé, le stage est clôturé et une attestation PDF officielle est générée.";

        // Merci
        if (q.Contains("merci") || q.Contains("thank") || q.Contains("au revoir") || q.Contains("bye") || q.Contains("bonne journée"))
            return "Avec plaisir ! N'hésitez pas à revenir si vous avez d'autres questions. Bonne continuation ! 😊";

        // Default adapté au rôle
        if (isRH) return "Je peux vous aider sur : **publier des offres**, **gérer les candidatures**, **signer des conventions**, **assigner des encadrants** ou **valider des rapports**. Que souhaitez-vous savoir ?";
        if (isSchool) return "Je peux vous aider sur : **consulter les candidatures**, **générer la convention** ou **signer la convention**. Que souhaitez-vous savoir ?";
        if (isEncadrant) return "Je peux vous aider sur : **gérer vos stagiaires**, **assigner des tâches**, **réaliser des évaluations** ou **consulter les rapports**. Que souhaitez-vous savoir ?";
        if (isAdmin) return "Je peux vous aider sur : **gestion des utilisateurs**, **statistiques**, **logs d'activité** ou **configuration de la plateforme**. Que souhaitez-vous savoir ?";
        return "Je suis l'assistant de la plateforme **Gestion des Stages MEN**. Je peux vous renseigner sur : les **offres**, la **candidature**, les **conventions**, les **rapports**, les **tâches** ou le **workflow complet**. Que souhaitez-vous savoir ?";
    }
}

public class ChatbotRequest
{
    public string Question { get; set; } = string.Empty;
    public string? Role { get; set; }
    public List<ChatMessage> History { get; set; } = new();
}

public class ChatMessage
{
    public string Role { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}

public class SentimentRequest
{
    public string Text { get; set; } = string.Empty;
    public string? StudentName { get; set; }
    public string? SupervisorName { get; set; }
}

