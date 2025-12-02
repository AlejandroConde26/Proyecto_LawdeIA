using LawdeIA.Data;
using LawdeIA.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace LawdeIA.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly LawdeIAContext _context;
        private readonly ILogger<AdminController> _logger;
        private readonly HttpClient _httpClient;

        // CONFIGURACIÓN
        private const string GEMINI_API_KEY = "AIzaSyAWFhRSrUJJRQpMGLhhqPwaIPVqry6esCg";
        private const string GEMINI_CHAT_MODEL = "gemini-2.0-flash";
        private const string GEMINI_EMBEDDING_MODEL = "text-embedding-004";

        public AdminController(LawdeIAContext context, ILogger<AdminController> logger)
        {
            _context = context;
            _logger = logger;
            _httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
        }

        public IActionResult Index() => View();

        // --- GESTIÓN DE DOCUMENTOS PÚBLICOS (BASE DE CONOCIMIENTO) ---
        [HttpGet]
        public async Task<IActionResult> GetPublicDocuments()
        {
            var documents = await _context.RAGDocuments
                .Where(d => d.Status == "Active" &&
                           d.AccessLevel == "Public") // Solo documentos públicos
                .OrderByDescending(d => d.LastUpdated)
                .Select(d => new {
                    id = d.DocumentID,
                    title = d.Title,
                    fileName = d.FileName,
                    createdAt = d.CreatedAt.ToString("dd/MM/yyyy"),
                    lastUpdated = d.LastUpdated.ToString("dd/MM/yyyy"),
                    source = d.Source,
                    fileSize = d.FileSize,
                    fileType = d.FileType,
                    chunkCount = d.ChunkCount,
                    contentLength = d.Content.Length,
                    processingStatus = d.ProcessingStatus,
                    accessLevel = d.AccessLevel,
                    uploader = d.User.Username,
                    usageCount = d.Conversations.Count(c => c.Status == "Active")
                })
                .ToListAsync();

            return Json(documents);
        }

        [HttpGet]
        public async Task<IActionResult> GetDocumentInfo(int documentId)
        {
            var document = await _context.RAGDocuments
                .Include(d => d.User)
                .Where(d => d.DocumentID == documentId &&
                           d.Status == "Active" &&
                           d.AccessLevel == "Public") // Solo documentos públicos
                .Select(d => new {
                    id = d.DocumentID,
                    title = d.Title,
                    fileName = d.FileName,
                    createdAt = d.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                    lastUpdated = d.LastUpdated.ToString("dd/MM/yyyy HH:mm"),
                    fileSize = d.FileSize,
                    fileType = d.FileType,
                    chunkCount = d.ChunkCount,
                    contentLength = d.Content.Length,
                    contentSummary = d.ContentSummary,
                    processingStatus = d.ProcessingStatus,
                    embeddingModel = d.EmbeddingModel,
                    accessLevel = d.AccessLevel,
                    uploader = d.User.Username,
                    uploaderId = d.UserID,
                    usageCount = d.Conversations.Count(c => c.Status == "Active"),
                    chunkDetails = d.Chunks.Select(c => new {
                        index = c.ChunkIndex,
                        contentLength = c.Content.Length,
                        tokenCount = c.TokenCount
                    }).OrderBy(c => c.index).ToList()
                })
                .FirstOrDefaultAsync();

            if (document == null)
                return Json(new { success = false, error = "Documento no encontrado o no es público" });

            return Json(new { success = true, document });
        }

        [HttpPost]
        [RequestSizeLimit(100_000_000)] // 100MB
        public async Task<IActionResult> UploadDocument(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return Json(new { success = false, error = "Archivo vacío" });

            try
            {
                var userId = GetUserId();
                string textContent = "";
                string ext = Path.GetExtension(file.FileName).ToLower();

                _logger.LogInformation($"📤 Admin subiendo documento público: {file.FileName}");

                // Verificar que el usuario sea admin
                var isAdmin = await IsUserAdmin(userId);
                if (!isAdmin)
                {
                    return Json(new { success = false, error = "Acceso denegado. Solo administradores pueden subir documentos públicos." });
                }

                // Crear documento público
                var doc = new RAGDocument
                {
                    UserID = userId,
                    Title = file.FileName.Length > 100 ? file.FileName.Substring(0, 100) : file.FileName,
                    FileName = file.FileName,
                    FileSize = file.Length,
                    FileType = ext.TrimStart('.'),
                    Source = "AdminUpload",
                    CreatedAt = DateTime.UtcNow,
                    LastUpdated = DateTime.UtcNow,
                    AccessLevel = "Public", // Documento público para base de conocimiento
                    Status = "Processing",
                    ProcessingStatus = "ExtractingText"
                };
                _context.RAGDocuments.Add(doc);
                await _context.SaveChangesAsync();

                await LogAudit(userId, "PublicDocumentUploaded", "RAGDocument", doc.DocumentID,
                    $"Documento público '{file.FileName}' subido a la base de conocimiento");

                // 1. EXTRACCIÓN DE TEXTO
                if (ext == ".pdf")
                {
                    try
                    {
                        _logger.LogInformation($"🔧 Extrayendo texto de PDF...");
                        using (var stream = file.OpenReadStream())
                        using (var pdf = PdfDocument.Open(stream))
                        {
                            var sb = new StringBuilder();
                            foreach (var page in pdf.GetPages())
                            {
                                var text = ContentOrderTextExtractor.GetText(page);
                                sb.Append(text + " ");
                            }
                            textContent = sb.ToString().Trim();
                        }
                        doc.ProcessingStatus = "TextExtracted";
                    }
                    catch (Exception pdfEx)
                    {
                        _logger.LogError(pdfEx, "❌ Error en extracción PDF local");
                        doc.ProcessingStatus = "ExtractionFailed";
                        await _context.SaveChangesAsync();
                        return Json(new { success = false, error = "Error extrayendo texto del PDF" });
                    }
                }
                else if (ext == ".txt" || ext == ".md")
                {
                    using (var reader = new StreamReader(file.OpenReadStream()))
                    {
                        textContent = await reader.ReadToEndAsync();
                        doc.ProcessingStatus = "TextExtracted";
                    }
                }

                // 2. OCR CON GEMINI PARA IMÁGENES/ESCANEOS
                if (string.IsNullOrWhiteSpace(textContent))
                {
                    if (ext == ".pdf" || ext == ".jpg" || ext == ".png" || ext == ".jpeg")
                    {
                        _logger.LogInformation($"👁️ Usando OCR de Gemini...");
                        doc.ProcessingStatus = "UsingOCR";
                        await _context.SaveChangesAsync();

                        textContent = await ExtractTextWithGemini(file);
                        if (!string.IsNullOrWhiteSpace(textContent))
                        {
                            doc.ProcessingStatus = "OCRExtracted";
                        }
                    }
                }

                if (string.IsNullOrWhiteSpace(textContent))
                {
                    doc.Status = "Error";
                    doc.ProcessingStatus = "NoContentExtracted";
                    await _context.SaveChangesAsync();
                    return Json(new { success = false, error = "No se pudo extraer contenido del archivo" });
                }

                // 3. ACTUALIZAR DOCUMENTO CON CONTENIDO
                doc.Content = textContent;
                doc.ContentSummary = GenerateContentSummary(textContent);
                doc.ProcessingStatus = "GeneratingEmbeddings";
                doc.LastUpdated = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // 4. GENERAR CHUNKS Y EMBEDDINGS
                _logger.LogInformation($"🧠 Generando embeddings para documento público...");
                var chunksCreated = await GenerateDocumentEmbeddings(doc.DocumentID, textContent);

                doc.ChunkCount = chunksCreated;
                doc.EmbeddingModel = GEMINI_EMBEDDING_MODEL;
                doc.Status = "Active";
                doc.ProcessingStatus = "Completed";
                doc.LastUpdated = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation($"✅ Documento público procesado: {doc.Title} - {chunksCreated} chunks");

                await LogAudit(userId, "PublicDocumentProcessed", "RAGDocument", doc.DocumentID,
                    $"Documento público procesado con {chunksCreated} chunks - Ahora disponible en la base de conocimiento");

                return Json(new
                {
                    success = true,
                    document = new
                    {
                        id = doc.DocumentID,
                        title = doc.Title,
                        chunkCount = doc.ChunkCount,
                        accessLevel = doc.AccessLevel,
                        uploader = "Admin"
                    },
                    message = "Documento añadido a la base de conocimiento global"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error en UploadDocument (Admin)");
                return Json(new { success = false, error = "Error procesando documento: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteDocument([FromBody] DeleteDocumentRequest req)
        {
            try
            {
                var userId = GetUserId();

                // Verificar que el usuario sea admin
                var isAdmin = await IsUserAdmin(userId);
                if (!isAdmin)
                {
                    return Json(new { success = false, error = "Acceso denegado. Solo administradores pueden eliminar documentos públicos." });
                }

                var document = await _context.RAGDocuments
                    .Include(d => d.Conversations)
                    .FirstOrDefaultAsync(d => d.DocumentID == req.DocumentId &&
                                             d.AccessLevel == "Public"); // Solo documentos públicos

                if (document == null)
                    return Json(new { success = false, error = "Documento público no encontrado" });

                // Verificar si el documento está siendo usado en conversaciones activas
                var activeConversations = document.Conversations
                    .Where(c => c.Status == "Active")
                    .Count();

                if (activeConversations > 0)
                {
                    return Json(new
                    {
                        success = false,
                        error = $"No se puede eliminar el documento. Está siendo usado en {activeConversations} conversación(es) activa(s) de usuarios.",
                        conversationsCount = activeConversations
                    });
                }

                // Marcar como eliminado
                document.Status = "Deleted";
                document.LastUpdated = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation($"✅ Documento público {req.DocumentId} marcado como eliminado");

                await LogAudit(userId, "PublicDocumentDeleted", "RAGDocument", req.DocumentId,
                    $"Documento público '{document.Title}' eliminado de la base de conocimiento");

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error en DeleteDocument (Admin)");
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        [HttpGet]
        public async Task<IActionResult> GetDocumentStatistics()
        {
            var stats = await _context.RAGDocuments
                .Where(d => d.AccessLevel == "Public" && d.Status == "Active")
                .GroupBy(d => 1)
                .Select(g => new
                {
                    totalDocuments = g.Count(),
                    totalChunks = g.Sum(d => d.ChunkCount),
                    totalSize = g.Sum(d => d.FileSize),
                    averageChunksPerDocument = g.Average(d => d.ChunkCount),
                    documentTypes = g.GroupBy(d => d.FileType)
                        .Select(grp => new
                        {
                            type = grp.Key,
                            count = grp.Count()
                        }).ToList(),
                    recentDocuments = g.OrderByDescending(d => d.CreatedAt)
                        .Take(5)
                        .Select(d => new
                        {
                            id = d.DocumentID,
                            title = d.Title,
                            createdAt = d.CreatedAt.ToString("dd/MM/yyyy"),
                            chunkCount = d.ChunkCount
                        }).ToList()
                })
                .FirstOrDefaultAsync();

            if (stats == null)
            {
                // Crear objeto por defecto con tipos explícitos
                return Json(new
                {
                    totalDocuments = 0,
                    totalChunks = 0,
                    totalSize = (long)0,
                    averageChunksPerDocument = 0.0,
                    documentTypes = new List<object>(),
                    recentDocuments = new List<object>()
                });
            }

            return Json(stats);
        }
        // --- HELPERS PRIVADOS ---
        private int GetUserId() => int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int id) ? id : 0;

        private async Task<bool> IsUserAdmin(int userId)
        {
            var user = await _context.Users
                .Where(u => u.UserID == userId)
                .Select(u => new { u.Role })
                .FirstOrDefaultAsync();

            return user?.Role == "Admin";
        }

        private async Task<int> GenerateDocumentEmbeddings(int documentId, string textContent)
        {
            var chunks = SplitTextIntoChunks(textContent);
            int chunksCreated = 0;

            _logger.LogInformation($"✂️ Dividiendo texto en {chunks.Count} chunks...");

            // Eliminar chunks existentes (por si hay reprocesamiento)
            var existingChunks = await _context.RAGDocumentChunks
                .Where(c => c.DocumentID == documentId)
                .ToListAsync();
            _context.RAGDocumentChunks.RemoveRange(existingChunks);

            // Crear nuevos chunks
            for (int i = 0; i < chunks.Count && i < 150; i++) // Límite de 150 chunks para documentos públicos
            {
                var chunk = chunks[i];
                if (string.IsNullOrWhiteSpace(chunk) || chunk.Length < 20) continue;

                var documentChunk = new RAGDocumentChunk
                {
                    DocumentID = documentId,
                    ChunkIndex = i,
                    Content = chunk,
                    ContentHash = ComputeHash(chunk),
                    TokenCount = EstimateTokens(chunk),
                    CreatedAt = DateTime.UtcNow
                };
                _context.RAGDocumentChunks.Add(documentChunk);
                await _context.SaveChangesAsync();

                // Generar embedding para el chunk
                var vector = await GetEmbedding(chunk);
                if (vector != null && vector.Length > 0)
                {
                    var embedding = new RAGEmbedding
                    {
                        ChunkID = documentChunk.ChunkID,
                        DocumentID = documentId,
                        Vector = VectorToBytes(vector),
                        VectorDimensions = (short)vector.Length,
                        ChunkIndex = i,
                        ModelUsed = GEMINI_EMBEDDING_MODEL,
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true
                    };
                    _context.RAGEmbeddings.Add(embedding);
                    chunksCreated++;
                }

                // Guardar cada 5 chunks
                if (i % 5 == 0)
                {
                    await _context.SaveChangesAsync();
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation($"🎉 Embeddings completados para documento público: {chunksCreated} chunks");

            return chunksCreated;
        }

        private List<string> SplitTextIntoChunks(string text, int maxChunkSize = 800)
        {
            var chunks = new List<string>();
            if (string.IsNullOrWhiteSpace(text)) return chunks;

            // Dividir por párrafos
            var paragraphs = text.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var paragraph in paragraphs)
            {
                var trimmed = paragraph.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;

                if (trimmed.Length <= maxChunkSize)
                {
                    chunks.Add(trimmed);
                }
                else
                {
                    // Dividir párrafo largo
                    var sentences = trimmed.Split(new[] { '.', '!', '?', ';', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    var currentChunk = new StringBuilder();

                    foreach (var sentence in sentences)
                    {
                        var trimmedSentence = sentence.Trim();
                        if (string.IsNullOrEmpty(trimmedSentence)) continue;

                        if (currentChunk.Length + trimmedSentence.Length > maxChunkSize && currentChunk.Length > 0)
                        {
                            chunks.Add(currentChunk.ToString());
                            currentChunk.Clear();
                        }

                        if (currentChunk.Length > 0)
                            currentChunk.Append(". ");
                        currentChunk.Append(trimmedSentence);
                    }

                    if (currentChunk.Length > 0)
                        chunks.Add(currentChunk.ToString());
                }
            }

            return chunks.Where(c => !string.IsNullOrWhiteSpace(c) && c.Length > 10).ToList();
        }

        private async Task<float[]> GetEmbedding(string text)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(text)) return null;

                var cleanText = text.Replace("\n", " ").Substring(0, Math.Min(text.Length, 10000));

                var url = $"https://generativelanguage.googleapis.com/v1beta/models/{GEMINI_EMBEDDING_MODEL}:embedContent?key={GEMINI_API_KEY}";
                var body = new
                {
                    model = $"models/{GEMINI_EMBEDDING_MODEL}",
                    content = new
                    {
                        parts = new[] {
                            new {
                                text = cleanText
                            }
                        }
                    }
                };

                var jsonContent = JsonSerializer.Serialize(body);
                var response = await _httpClient.PostAsync(url,
                    new StringContent(jsonContent, Encoding.UTF8, "application/json"));

                if (!response.IsSuccessStatusCode) return null;

                var responseContent = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<GeminiEmbeddingResponse>(
                    responseContent,
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

                return data?.Embedding?.Values;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error obteniendo embedding");
                return null;
            }
        }

        private async Task LogAudit(int? userId, string action, string entityType, long? entityId, string details)
        {
            var auditLog = new AuditLog
            {
                UserID = userId,
                Action = action,
                EntityType = entityType,
                EntityID = entityId,
                Details = details,
                IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = Request.Headers["User-Agent"].ToString(),
                CreatedAt = DateTime.UtcNow
            };
            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }

        private byte[] ComputeHash(string text)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            return sha256.ComputeHash(Encoding.UTF8.GetBytes(text));
        }

        private byte[] VectorToBytes(float[] vector)
        {
            var byteArray = new byte[vector.Length * 4];
            Buffer.BlockCopy(vector, 0, byteArray, 0, byteArray.Length);
            return byteArray;
        }

        private int EstimateTokens(string text)
        {
            // Estimación aproximada: 1 token ≈ 4 caracteres
            return (int)Math.Ceiling(text.Length / 4.0);
        }

        private string GenerateContentSummary(string content)
        {
            if (content.Length <= 200) return content;
            return content.Substring(0, 200) + "...";
        }

        private async Task<string> ExtractTextWithGemini(IFormFile file)
        {
            try
            {
                string base64Data;
                using (var ms = new MemoryStream())
                {
                    await file.CopyToAsync(ms);
                    base64Data = Convert.ToBase64String(ms.ToArray());
                }

                string mimeType = GetMimeType(file.FileName);
                var url = $"https://generativelanguage.googleapis.com/v1beta/models/{GEMINI_CHAT_MODEL}:generateContent?key={GEMINI_API_KEY}";

                var body = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new object[]
                            {
                                new { text = "Extrae y transcribe TODO el texto de este documento de manera precisa y completa." },
                                new { inline_data = new { mime_type = mimeType, data = base64Data } }
                            }
                        }
                    },
                    generationConfig = new { temperature = 0.1, maxOutputTokens = 4096 }
                };

                var response = await _httpClient.PostAsync(url,
                    new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"));

                if (!response.IsSuccessStatusCode) return null;

                var resultJson = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<GeminiResponse>(
                    resultJson,
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

                return result?.Candidates?[0]?.Content?.Parts?[0]?.Text;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en ExtractTextWithGemini");
                return null;
            }
        }

        private string GetMimeType(string fileName)
        {
            var ext = Path.GetExtension(fileName).ToLower();
            return ext switch
            {
                ".pdf" => "application/pdf",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                _ => "application/octet-stream"
            };
        }

        // DTOs
        public class DeleteDocumentRequest
        {
            public int DocumentId { get; set; }
        }

        public class GeminiEmbeddingResponse
        {
            public GeminiEmbeddingVal? Embedding { get; set; }
        }

        public class GeminiEmbeddingVal
        {
            public float[]? Values { get; set; }
        }

        public class GeminiResponse
        {
            public GeminiCandidate[]? Candidates { get; set; }
        }

        public class GeminiCandidate
        {
            public GeminiContent? Content { get; set; }
        }

        public class GeminiContent
        {
            public GeminiPart[]? Parts { get; set; }
        }

        public class GeminiPart
        {
            public string? Text { get; set; }
        }
    }
}