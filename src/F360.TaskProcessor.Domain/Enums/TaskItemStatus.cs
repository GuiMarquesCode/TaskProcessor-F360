using System.Text.Json.Serialization;

namespace F360.TaskProcessor.Domain.Enums;

/// <summary>
/// Representa os possíveis estados do ciclo de vida de uma tarefa (Requisito 3.a).
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TaskItemStatus
{
    Pendente = 0,
    EmProcessamento = 1,
    Concluido = 2,
    Erro = 3
}
