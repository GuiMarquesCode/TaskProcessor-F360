using System.Text.Json;
using F360.TaskProcessor.Application.Interfaces;
using F360.TaskProcessor.Domain.Entities;

namespace F360.TaskProcessor.Application.Strategies;

/// <summary>
/// Implementação auxiliar injetada via COMPOSIÇÃO nos processadores de tarefa.
/// Permite simular tempo de execução (para visualizar o status 'EmProcessamento' na API)
/// e disparar falhas controladas via payload JSON (ex.: {"simularFalha": true}).
/// </summary>
public class TaskSimulationHelper : ITaskSimulationHelper
{
    public async Task SimulateExecutionAndCheckFailureAsync(
        JobTask task,
        TimeSpan defaultDuration,
        CancellationToken cancellationToken = default)
    {
        bool shouldSimulateFailure = false;
        int delayMilliseconds = (int)defaultDuration.TotalMilliseconds;

        try
        {
            using var document = JsonDocument.Parse(task.DataJson);
            var root = document.RootElement;

            if (root.ValueKind == JsonValueKind.Object)
            {
                if (root.TryGetProperty("simularFalha", out var failProp) &&
                    failProp.ValueKind == JsonValueKind.True)
                {
                    shouldSimulateFailure = true;
                }

                if (root.TryGetProperty("tempoSimulacaoMs", out var delayProp) &&
                    delayProp.TryGetInt32(out int customDelay) &&
                    customDelay >= 0 && customDelay <= 30000)
                {
                    delayMilliseconds = customDelay;
                }
            }
        }
        catch (JsonException)
        {
            // Se o JSON não contiver propriedades extras de simulação, segue o fluxo padrão
        }

        if (delayMilliseconds > 0)
        {
            await Task.Delay(delayMilliseconds, cancellationToken);
        }

        if (shouldSimulateFailure)
        {
            throw new InvalidOperationException(
                $"Falha simulada durante o processamento da tarefa '{task.Type}' (Tentativa {task.Attempts}/{task.MaxAttempts}).");
        }
    }
}
