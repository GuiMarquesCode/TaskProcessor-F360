using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using F360.TaskProcessor.Domain.Entities;

namespace F360.TaskProcessor.Application.DTOs;

/// <summary>
/// Dados de entrada para criação de uma nova tarefa via API (Requisito 1.b).
/// </summary>
public class CreateTaskRequest
{
    /// <summary>
    /// Tipo da tarefa (ex.: "EnviarEmail", "GerarRelatorio").
    /// </summary>
    [Required(ErrorMessage = "O campo 'tipo' é obrigatório.")]
    public string Tipo { get; set; } = string.Empty;

    /// <summary>
    /// Dados necessários para o processamento em formato JSON.
    /// Dica para demonstração: envie {"simularFalha": true} para testar o fluxo de re-tentativas.
    /// </summary>
    [Required(ErrorMessage = "O campo 'dados' (JSON) é obrigatório.")]
    public JsonElement Dados { get; set; }

    /// <summary>
    /// Limite máximo de tentativas em caso de falha (Padrão = 3).
    /// </summary>
    [Range(1, 10, ErrorMessage = "O limite máximo de tentativas deve estar entre 1 e 10.")]
    public int MaxTentativas { get; set; } = JobTask.DefaultMaxAttempts;
}
