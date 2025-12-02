using LawdeIA.Models;
using Microsoft.EntityFrameworkCore;

namespace LawdeIA.Data
{
    public class LawdeIAContext : DbContext
    {
        public LawdeIAContext(DbContextOptions<LawdeIAContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<UserSession> UserSessions { get; set; }
        public DbSet<Conversation> Conversations { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<ConversationMetadata> ConversationMetadata { get; set; }
        public DbSet<RAGDocument> RAGDocuments { get; set; }
        public DbSet<RAGDocumentChunk> RAGDocumentChunks { get; set; }
        public DbSet<RAGEmbedding> RAGEmbeddings { get; set; }
        public DbSet<RAGSearchCache> RAGSearchCaches { get; set; }
        public DbSet<AIModel> AIModels { get; set; }
        public DbSet<UsageMetric> UsageMetrics { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<SystemConfig> SystemConfigs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuraciones de precisión decimal
            modelBuilder.Entity<AIModel>()
                .Property(a => a.CostPerToken)
                .HasPrecision(18, 12); // 18 dígitos totales, 12 decimales

            modelBuilder.Entity<RAGSearchCache>()
                .Property(r => r.MaxSimilarity)
                .HasPrecision(5, 4); // 5 dígitos totales, 4 decimales (ej: 0.9999)

            modelBuilder.Entity<UsageMetric>()
                .Property(u => u.CostIncurred)
                .HasPrecision(18, 12); // 18 dígitos totales, 12 decimales

            // Configuraciones de índices para mejor rendimiento
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<UserSession>()
                .HasIndex(u => u.Token)
                .IsUnique();

            modelBuilder.Entity<UserSession>()
                .HasIndex(u => u.ExpiresAt);

            modelBuilder.Entity<Conversation>()
                .HasIndex(c => new { c.UserID, c.LastUpdated });

            modelBuilder.Entity<Conversation>()
                .HasIndex(c => c.Status);

            modelBuilder.Entity<Message>()
                .HasIndex(m => new { m.ConversationID, m.CreatedAt });

            modelBuilder.Entity<Message>()
                .HasIndex(m => m.ContentHash);

            modelBuilder.Entity<RAGDocument>()
                .HasIndex(d => new { d.UserID, d.Status });

            modelBuilder.Entity<RAGDocument>()
                .HasIndex(d => d.LastAccessed);

            modelBuilder.Entity<RAGDocumentChunk>()
                .HasIndex(c => new { c.DocumentID, c.ChunkIndex })
                .IsUnique();

            modelBuilder.Entity<RAGEmbedding>()
                .HasIndex(e => new { e.DocumentID, e.IsActive });

            modelBuilder.Entity<RAGEmbedding>()
                .HasIndex(e => e.ChunkID);

            modelBuilder.Entity<RAGSearchCache>()
                .HasIndex(c => new { c.DocumentID, c.QueryHash })
                .IsUnique();

            modelBuilder.Entity<RAGSearchCache>()
                .HasIndex(c => c.ExpiresAt);

            modelBuilder.Entity<UsageMetric>()
                .HasIndex(u => new { u.UserID, u.CreatedAt });

            modelBuilder.Entity<AuditLog>()
                .HasIndex(a => new { a.UserID, a.CreatedAt });

            modelBuilder.Entity<SystemConfig>()
                .HasIndex(s => s.ConfigKey)
                .IsUnique();

            // Configuraciones de relaciones
            modelBuilder.Entity<User>()
                .HasMany(u => u.Conversations)
                .WithOne(c => c.User)
                .HasForeignKey(c => c.UserID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<User>()
                .HasMany(u => u.Documents)
                .WithOne(d => d.User)
                .HasForeignKey(d => d.UserID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<User>()
                .HasMany(u => u.Sessions)
                .WithOne(s => s.User)
                .HasForeignKey(s => s.UserID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Conversation>()
                .HasOne(c => c.Metadata)
                .WithOne(m => m.Conversation)
                .HasForeignKey<ConversationMetadata>(m => m.ConversationID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Conversation>()
                .HasMany(c => c.Messages)
                .WithOne(m => m.Conversation)
                .HasForeignKey(m => m.ConversationID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Message>()
                .HasOne(m => m.ParentMessage)
                .WithMany(m => m.ChildMessages)
                .HasForeignKey(m => m.ParentMessageID)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<RAGDocument>()
                .HasMany(d => d.Chunks)
                .WithOne(c => c.Document)
                .HasForeignKey(c => c.DocumentID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RAGDocument>()
                .HasMany(d => d.Embeddings)
                .WithOne(e => e.Document)
                .HasForeignKey(e => e.DocumentID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RAGDocument>()
                .HasMany(d => d.Conversations)
                .WithOne(c => c.SelectedDocument)
                .HasForeignKey(c => c.SelectedDocumentID)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<RAGDocumentChunk>()
                .HasMany(c => c.Embeddings)
                .WithOne(e => e.Chunk)
                .HasForeignKey(e => e.ChunkID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UsageMetric>()
                .HasOne(u => u.User)
                .WithMany()
                .HasForeignKey(u => u.UserID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UsageMetric>()
                .HasOne(u => u.Model)
                .WithMany()
                .HasForeignKey(u => u.ModelID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AuditLog>()
                .HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserID)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<SystemConfig>()
                .HasOne(s => s.UpdatedByUser)
                .WithMany()
                .HasForeignKey(s => s.UpdatedBy)
                .OnDelete(DeleteBehavior.SetNull);

            // Configuraciones de longitud de cadena para SQL Server
            modelBuilder.Entity<Message>()
                .Property(m => m.Content)
                .HasColumnType("nvarchar(MAX)");

            modelBuilder.Entity<RAGDocument>()
                .Property(d => d.Content)
                .HasColumnType("nvarchar(MAX)");

            modelBuilder.Entity<RAGDocument>()
                .Property(d => d.Metadata)
                .HasColumnType("nvarchar(MAX)");

            modelBuilder.Entity<RAGDocumentChunk>()
                .Property(c => c.Content)
                .HasMaxLength(4000);

            modelBuilder.Entity<RAGSearchCache>()
                .Property(c => c.Results)
                .HasColumnType("nvarchar(MAX)");

            modelBuilder.Entity<ConversationMetadata>()
                .Property(c => c.Parameters)
                .HasColumnType("nvarchar(MAX)");

            modelBuilder.Entity<AuditLog>()
                .Property(a => a.Details)
                .HasColumnType("nvarchar(MAX)");

            modelBuilder.Entity<SystemConfig>()
                .Property(s => s.ConfigValue)
                .HasColumnType("nvarchar(MAX)");

            // Configuración de columna Vector como varbinary
            modelBuilder.Entity<RAGEmbedding>()
                .Property(e => e.Vector)
                .HasColumnType("varbinary(MAX)");

            // Configuración de contenido hash
            modelBuilder.Entity<Message>()
                .Property(m => m.ContentHash)
                .HasColumnType("varbinary(64)");

            modelBuilder.Entity<RAGDocumentChunk>()
                .Property(c => c.ContentHash)
                .HasColumnType("varbinary(64)");

            modelBuilder.Entity<RAGSearchCache>()
                .Property(c => c.QueryHash)
                .HasColumnType("varbinary(64)");

            // Valores por defecto
            modelBuilder.Entity<User>()
                .Property(u => u.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity<User>()
                .Property(u => u.Role)
                .HasDefaultValue("User");

            modelBuilder.Entity<User>()
                .Property(u => u.SubscriptionLevel)
                .HasDefaultValue("Free");

            modelBuilder.Entity<User>()
                .Property(u => u.IsActive)
                .HasDefaultValue(true);

            modelBuilder.Entity<Conversation>()
                .Property(c => c.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity<Conversation>()
                .Property(c => c.LastUpdated)
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity<Conversation>()
                .Property(c => c.Status)
                .HasDefaultValue("Active");

            modelBuilder.Entity<Conversation>()
                .Property(c => c.IsPinned)
                .HasDefaultValue(false);

            modelBuilder.Entity<Message>()
                .Property(m => m.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity<Message>()
                .Property(m => m.IsEdited)
                .HasDefaultValue(false);

            modelBuilder.Entity<RAGDocument>()
                .Property(d => d.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity<RAGDocument>()
                .Property(d => d.LastUpdated)
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity<RAGDocument>()
                .Property(d => d.AccessLevel)
                .HasDefaultValue("Private");

            modelBuilder.Entity<RAGDocument>()
                .Property(d => d.Status)
                .HasDefaultValue("Active");

            modelBuilder.Entity<RAGDocumentChunk>()
                .Property(c => c.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity<RAGEmbedding>()
                .Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity<RAGEmbedding>()
                .Property(e => e.IsActive)
                .HasDefaultValue(true);

            modelBuilder.Entity<RAGEmbedding>()
                .Property(e => e.EmbeddingVersion)
                .HasDefaultValue("v1");

            modelBuilder.Entity<RAGSearchCache>()
                .Property(c => c.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity<AIModel>()
                .Property(a => a.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity<AIModel>()
                .Property(a => a.IsActive)
                .HasDefaultValue(true);

            modelBuilder.Entity<UsageMetric>()
                .Property(u => u.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity<AuditLog>()
                .Property(a => a.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity<SystemConfig>()
                .Property(s => s.UpdatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity<SystemConfig>()
                .Property(s => s.DataType)
                .HasDefaultValue("String");
        }
    }
}