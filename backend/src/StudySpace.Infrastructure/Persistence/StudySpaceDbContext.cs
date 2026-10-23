using Microsoft.EntityFrameworkCore;

namespace StudySpace.Infrastructure.Persistence;

public class StudySpaceDbContext(DbContextOptions<StudySpaceDbContext> options) : DbContext(options)
{
    public DbSet<ExamEntity> Exams => Set<ExamEntity>();
    public DbSet<DomainEntity> Domains => Set<DomainEntity>();
    public DbSet<TopicEntity> Topics => Set<TopicEntity>();
    public DbSet<LessonEntity> Lessons => Set<LessonEntity>();
    public DbSet<ScenarioEntity> Scenarios => Set<ScenarioEntity>();
    public DbSet<QuestionEntity> Questions => Set<QuestionEntity>();
    public DbSet<QuestionOptionEntity> QuestionOptions => Set<QuestionOptionEntity>();
    public DbSet<FlashcardEntity> Flashcards => Set<FlashcardEntity>();
    public DbSet<ContentPackEntity> ContentPacks => Set<ContentPackEntity>();
    public DbSet<AttemptEntity> Attempts => Set<AttemptEntity>();
    public DbSet<AttemptQuestionEntity> AttemptQuestions => Set<AttemptQuestionEntity>();
    public DbSet<AnswerRecordEntity> AnswerRecords => Set<AnswerRecordEntity>();
    public DbSet<BankItemEntity> BankItems => Set<BankItemEntity>();
    public DbSet<FlashcardScheduleEntity> FlashcardSchedules => Set<FlashcardScheduleEntity>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<ExamEntity>().HasIndex(e => e.Code).IsUnique();

        b.Entity<DomainEntity>().HasIndex(d => new { d.ExamId, d.Slug }).IsUnique();
        b.Entity<DomainEntity>().HasOne<ExamEntity>().WithMany().HasForeignKey(d => d.ExamId).OnDelete(DeleteBehavior.Cascade);

        b.Entity<TopicEntity>().HasIndex(t => new { t.ExamId, t.Slug }).IsUnique();
        b.Entity<TopicEntity>().HasOne<ExamEntity>().WithMany().HasForeignKey(t => t.ExamId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<TopicEntity>().HasOne<DomainEntity>().WithMany().HasForeignKey(t => t.DomainId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<TopicEntity>().HasOne(t => t.Lesson).WithOne().HasForeignKey<LessonEntity>(l => l.TopicId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<TopicEntity>().HasMany(t => t.Scenarios).WithOne().HasForeignKey(s => s.TopicId).OnDelete(DeleteBehavior.Cascade);

        b.Entity<ScenarioEntity>().HasIndex(s => new { s.TopicId, s.Slug }).IsUnique();

        b.Entity<QuestionEntity>().HasIndex(q => new { q.ExamId, q.Slug }).IsUnique();
        b.Entity<QuestionEntity>().HasOne<ExamEntity>().WithMany().HasForeignKey(q => q.ExamId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<QuestionEntity>().HasOne<TopicEntity>().WithMany().HasForeignKey(q => q.TopicId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<QuestionEntity>().HasOne<ScenarioEntity>().WithMany().HasForeignKey(q => q.ScenarioId).OnDelete(DeleteBehavior.SetNull);
        b.Entity<QuestionEntity>().HasOne<ContentPackEntity>().WithMany().HasForeignKey(q => q.PackId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<QuestionEntity>().HasMany(q => q.Options).WithOne().HasForeignKey(o => o.QuestionId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<QuestionEntity>().Property(q => q.VerificationStatus).HasConversion<string>();

        b.Entity<FlashcardEntity>().HasIndex(f => new { f.ExamId, f.Slug }).IsUnique();
        b.Entity<FlashcardEntity>().HasOne<ExamEntity>().WithMany().HasForeignKey(f => f.ExamId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<FlashcardEntity>().HasOne<ContentPackEntity>().WithMany().HasForeignKey(f => f.PackId).OnDelete(DeleteBehavior.Restrict);

        b.Entity<ContentPackEntity>().HasIndex(p => new { p.PackSlug, p.Version }).IsUnique();
        b.Entity<ContentPackEntity>().HasOne<ExamEntity>().WithMany().HasForeignKey(p => p.ExamId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<ContentPackEntity>().Property(p => p.VerificationStatus).HasConversion<string>();

        b.Entity<AttemptEntity>().HasOne<ExamEntity>().WithMany().HasForeignKey(a => a.ExamId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<AttemptEntity>().HasMany(a => a.Questions).WithOne().HasForeignKey(q => q.AttemptId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<AttemptQuestionEntity>().HasIndex(q => new { q.AttemptId, q.QuestionId }).IsUnique();
        b.Entity<AttemptQuestionEntity>().HasOne<QuestionEntity>().WithMany().HasForeignKey(q => q.QuestionId).OnDelete(DeleteBehavior.Cascade);

        b.Entity<AnswerRecordEntity>().HasIndex(a => new { a.AttemptId, a.QuestionId }).IsUnique();
        b.Entity<AnswerRecordEntity>().HasOne<AttemptEntity>().WithMany().HasForeignKey(a => a.AttemptId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<AnswerRecordEntity>().HasOne<QuestionEntity>().WithMany().HasForeignKey(a => a.QuestionId).OnDelete(DeleteBehavior.Cascade);

        b.Entity<BankItemEntity>().HasKey(i => i.QuestionId);
        b.Entity<BankItemEntity>().HasOne<QuestionEntity>().WithOne().HasForeignKey<BankItemEntity>(i => i.QuestionId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<BankItemEntity>().Property(i => i.Status).HasConversion<string>();

        b.Entity<FlashcardScheduleEntity>().HasKey(s => s.FlashcardId);
        b.Entity<FlashcardScheduleEntity>().HasOne<FlashcardEntity>().WithOne().HasForeignKey<FlashcardScheduleEntity>(s => s.FlashcardId).OnDelete(DeleteBehavior.Cascade);
    }
}
