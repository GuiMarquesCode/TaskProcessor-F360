using F360.TaskProcessor.Domain.Enums;
using F360.TaskProcessor.Domain.Exceptions;

namespace F360.TaskProcessor.Domain.Entities;

/// <summary>
/// Entidade de domínio que representa uma tarefa (Job) a ser processada em background.
/// Aplica o pilar de ENCAPSULAMENTO (POO) e Single Responsibility Principle (SRP):
/// todas as alterações de estado e contagem de tentativas ocorrem exclusivamente
/// através de métodos de negócio da própria entidade.
/// </summary>
public class JobTask
{
    public const int DefaultMaxAttempts = 3;

    public Guid Id { get; private set; }
    public string Type { get; private set; }
    public string DataJson { get; private set; }
    public TaskItemStatus Status { get; private set; }
    public int Attempts { get; private set; }
    public int MaxAttempts { get; private set; }
    public string? LastError { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    /// <summary>
    /// Construtor utilizado para criação de uma nova tarefa no domínio.
    /// </summary>
    public JobTask(string type, string dataJson, int maxAttempts = DefaultMaxAttempts)
    {
        if (string.IsNullOrWhiteSpace(type))
            throw new DomainException("O tipo da tarefa é obrigatório (ex.: 'EnviarEmail', 'GerarRelatorio').");

        if (string.IsNullOrWhiteSpace(dataJson))
            throw new DomainException("Os dados necessários para o processamento (JSON) são obrigatórios.");

        if (maxAttempts <= 0)
            throw new DomainException("O limite máximo de tentativas deve ser maior que zero.");

        Id = Guid.NewGuid();
        Type = type.Trim();
        DataJson = dataJson.Trim();
        Status = TaskItemStatus.Pendente;
        Attempts = 0;
        MaxAttempts = maxAttempts;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Construtor para reconstituição da entidade a partir da camada de persistência (MongoDB).
    /// </summary>
    public JobTask(
        Guid id,
        string type,
        string dataJson,
        TaskItemStatus status,
        int attempts,
        int maxAttempts,
        string? lastError,
        DateTime createdAt,
        DateTime updatedAt,
        DateTime? completedAt)
    {
        Id = id;
        Type = type;
        DataJson = dataJson;
        Status = status;
        Attempts = attempts;
        MaxAttempts = maxAttempts;
        LastError = lastError;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        CompletedAt = completedAt;
    }

    /// <summary>
    /// Verifica se a tarefa ainda possui tentativas disponíveis em caso de falha.
    /// </summary>
    public bool CanRetry() => Attempts < MaxAttempts;

    /// <summary>
    /// Inicia o processamento da tarefa, mudando o status para 'EmProcessamento' e incrementando a tentativa.
    /// </summary>
    public void MarkAsProcessing()
    {
        if (Status == TaskItemStatus.Concluido)
            throw new DomainException("Não é possível processar uma tarefa que já foi concluída.");

        if (Status == TaskItemStatus.Erro && !CanRetry())
            throw new DomainException("Não é possível processar uma tarefa que já atingiu o limite máximo de tentativas.");

        Status = TaskItemStatus.EmProcessamento;
        Attempts++;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marca a tarefa como concluída com sucesso.
    /// </summary>
    public void MarkAsCompleted()
    {
        if (Status != TaskItemStatus.EmProcessamento)
            throw new DomainException("Apenas tarefas com status 'EmProcessamento' podem ser marcadas como concluídas.");

        Status = TaskItemStatus.Concluido;
        LastError = null;
        UpdatedAt = DateTime.UtcNow;
        CompletedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Registra uma falha no processamento.
    /// Caso ainda existam tentativas restantes (Attempts &lt; MaxAttempts), retorna para 'Pendente' para re-tentativa.
    /// Caso tenha atingido o limite máximo de tentativas, define o status definitivo como 'Erro'.
    /// </summary>
    public void RegisterFailure(string errorMessage)
    {
        LastError = string.IsNullOrWhiteSpace(errorMessage)
            ? "Erro desconhecido durante o processamento da tarefa."
            : errorMessage.Trim();

        Status = CanRetry() ? TaskItemStatus.Pendente : TaskItemStatus.Erro;
        UpdatedAt = DateTime.UtcNow;
    }
}
