using F360.TaskProcessor.Domain.Entities;
using F360.TaskProcessor.Domain.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace F360.TaskProcessor.Infrastructure.Persistence;

/// <summary>
/// Modelo de documento para persistência no MongoDB.
/// Mantém a entidade de Domínio (JobTask) 100% limpa e livre de atributos de infraestrutura ([BsonId], etc.).
/// </summary>
public class TaskDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }

    [BsonElement("type")]
    public string Type { get; set; } = string.Empty;

    [BsonElement("dataJson")]
    public string DataJson { get; set; } = string.Empty;

    [BsonElement("status")]
    [BsonRepresentation(BsonType.String)]
    public TaskItemStatus Status { get; set; }

    [BsonElement("attempts")]
    public int Attempts { get; set; }

    [BsonElement("maxAttempts")]
    public int MaxAttempts { get; set; }

    [BsonElement("lastError")]
    public string? LastError { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; }

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; }

    [BsonElement("completedAt")]
    public DateTime? CompletedAt { get; set; }

    public static TaskDocument FromDomain(JobTask entity) => new()
    {
        Id = entity.Id,
        Type = entity.Type,
        DataJson = entity.DataJson,
        Status = entity.Status,
        Attempts = entity.Attempts,
        MaxAttempts = entity.MaxAttempts,
        LastError = entity.LastError,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt,
        CompletedAt = entity.CompletedAt
    };

    public JobTask ToDomain() => new(
        id: Id,
        type: Type,
        dataJson: DataJson,
        status: Status,
        attempts: Attempts,
        maxAttempts: MaxAttempts,
        lastError: LastError,
        createdAt: CreatedAt,
        updatedAt: UpdatedAt,
        completedAt: CompletedAt);
}
