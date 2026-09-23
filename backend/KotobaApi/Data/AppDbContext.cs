using KotobaApi.Models;
using Microsoft.EntityFrameworkCore;

namespace KotobaApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Deck> Decks => Set<Deck>();
    public DbSet<Word> Words => Set<Word>();
    public DbSet<WordProgress> WordProgresses => Set<WordProgress>();
    public DbSet<WordReviewEvent> WordReviewEvents => Set<WordReviewEvent>();
    public DbSet<GenerationRequest> GenerationRequests => Set<GenerationRequest>();
    public DbSet<GenerationRequestWord> GenerationRequestWords => Set<GenerationRequestWord>();
    public DbSet<GeneratedText> GeneratedTexts => Set<GeneratedText>();
    public DbSet<GeneratedTextWordUsage> GeneratedTextWordUsages => Set<GeneratedTextWordUsage>();
    public DbSet<ComprehensionQuestion> ComprehensionQuestions => Set<ComprehensionQuestion>();
    public DbSet<PracticeAttempt> PracticeAttempts => Set<PracticeAttempt>();
    public DbSet<UserAnswer> UserAnswers => Set<UserAnswer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<Word>()
            .Property(w => w.PartOfSpeech)
            .HasConversion<string>();
        
        modelBuilder.Entity<WordProgress>()
            .ToTable("word_progress");
        
        modelBuilder.Entity<WordProgress>()
            .HasIndex(p => new { p.UserId, p.WordId })
            .IsUnique();
        
        modelBuilder.Entity<WordReviewEvent>()
            .HasIndex(e => e.EventId)
            .IsUnique();

        modelBuilder.Entity<WordReviewEvent>()
            .Property(e => e.Source)
            .HasConversion<string>();

        modelBuilder.Entity<GenerationRequest>()
            .Property(r => r.Status)
            .HasConversion<string>();

        modelBuilder.Entity<GenerationRequest>()
            .Property(r => r.PromptParams)
            .HasColumnType("jsonb");

        modelBuilder.Entity<ComprehensionQuestion>()
            .Property(q => q.Options)
            .HasColumnType("jsonb");
    }
}