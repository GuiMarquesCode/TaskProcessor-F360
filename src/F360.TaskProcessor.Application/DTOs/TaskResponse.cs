using System.Text.Json;
using F360.TaskProcessor.Domain.Entities;

namespace F360.TaskProcessor.Application.DTOs;

/// <summary>
/// Representação de retorno de uma tarefa e seu status atual para consulta via API (Requisito 3.b).
/// </summary>
public class TaskResponse
{
    public Guid Id { get; init; }
    public string Tipo { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public int TentativasRealizadas { get; init; }
    public int MaximoTentativas { get; init; }
    public JsonElement Dados { get; init; }
    public string? UltimoErro { get; init; }
    public DateTime CriadoEm { get; init; }
    public DateTime AtualizadoEm { get; init; }
    public DateTime? ConcluidoEm { get; init; }

    public static TaskResponse FromEntity(JobTask entity)
    {
        JsonElement parsedData;
        try
        {
            using var doc = JsonDocument.Parse(entity.DataJson);
            parsedData = doc.RootElement.Clone();
        }
        catch
        {
            using var doc = JsonDocument.Parse(JsonSerializer.Serialize(new { raw = entity.DataJson }));
            parsedData = doc.RootElement.Clone();
        }

        return new TaskResponse
        {
            Id = entity.Id,
            Tipo = entity.Type,
            Status = entity.Status.ToString(),
            TentativasRealizadas = entity.Attempts,
            MaximoTentativas = entity.MaxAttempts,
            Dados = parsedData,
            UltimoErro = entity.LastError,
            CriadoEm = entity.CreatedAt,
            AtualizadoEm = entity.UpdatedAt,
            ConcluidoEm = entity.CompletedAt
        };
    }
}
