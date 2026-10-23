using StudySpace.Application.ContentPacks;

namespace StudySpace.Infrastructure.Persistence;

// Slug columns hold the ids from the content pack. They are the stable natural keys used to upsert.
// List-like fields (key points, links, tags, sources) are stored as JSON text arrays.

public class ExamEntity
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Version { get; set; } = "";
}

public class DomainEntity
{
    public int Id { get; set; }
    public int ExamId { get; set; }
    public string Slug { get; set; } = "";
    public string Name { get; set; } = "";
    public int WeightPercent { get; set; }
    public int SortOrder { get; set; }
}

public class TopicEntity
{
    public int Id { get; set; }
    public int ExamId { get; set; }
    public int DomainId { get; set; }
    public string Slug { get; set; } = "";
    public string Name { get; set; } = "";
    public int SortOrder { get; set; }
    public LessonEntity? Lesson { get; set; }
    public List<ScenarioEntity> Scenarios { get; set; } = [];
}

public class LessonEntity
{
    public int Id { get; set; }
    public int TopicId { get; set; }
    public string Summary { get; set; } = "";
    public string KeyPointsJson { get; set; } = "[]";
    public string DocLinksJson { get; set; } = "[]";
}

public class ScenarioEntity
{
    public int Id { get; set; }
    public int TopicId { get; set; }
    public string Slug { get; set; } = "";
    public string Text { get; set; } = "";
}

public class QuestionEntity
{
    public int Id { get; set; }
    public int ExamId { get; set; }
    public int TopicId { get; set; }
    public int? ScenarioId { get; set; }
    public string Slug { get; set; } = "";
    public string Type { get; set; } = "";
    public string Prompt { get; set; } = "";
    public string Explanation { get; set; } = "";
    public int Difficulty { get; set; }
    public string TagsJson { get; set; } = "[]";
    public string SourcesJson { get; set; } = "[]";
    public string? DataJson { get; set; }
    public int PackId { get; set; }
    public VerificationStatus VerificationStatus { get; set; }
    public List<QuestionOptionEntity> Options { get; set; } = [];
}

public class QuestionOptionEntity
{
    public int Id { get; set; }
    public int QuestionId { get; set; }
    public string Slug { get; set; } = "";
    public string Text { get; set; } = "";
    public bool IsCorrect { get; set; }
    public int SortOrder { get; set; }
}

public class FlashcardEntity
{
    public int Id { get; set; }
    public int ExamId { get; set; }
    public string Slug { get; set; } = "";
    public string Term { get; set; } = "";
    public string Definition { get; set; } = "";
    public int PackId { get; set; }
}

public class ContentPackEntity
{
    public int Id { get; set; }
    public int ExamId { get; set; }
    public string PackSlug { get; set; } = "";
    public string Version { get; set; } = "";
    public string FileHash { get; set; } = "";
    public DateTimeOffset ImportedAt { get; set; }
    public VerificationStatus VerificationStatus { get; set; }
}
