using F360.TaskProcessor.Application.Interfaces;

namespace F360.TaskProcessor.Application.Factories;

/// <summary>
/// Implementação do Factory Pattern que seleciona a estratégia de processamento correta
/// com base no tipo da tarefa.
/// Aplica o Open/Closed Principle (OCP): para adicionar um novo tipo de tarefa no sistema,
/// basta criar uma nova implementação de ITaskExecutionStrategy e registrá-la no container de DI,
/// sem precisar alterar esta classe com novos blocos if/switch.
/// </summary>
public class TaskStrategyFactory : ITaskStrategyFactory
{
    private readonly IEnumerable<ITaskExecutionStrategy> _strategies;

    public TaskStrategyFactory(IEnumerable<ITaskExecutionStrategy> strategies)
    {
        _strategies = strategies;
    }

    public ITaskExecutionStrategy GetStrategy(string taskType)
    {
        if (string.IsNullOrWhiteSpace(taskType))
            throw new ArgumentException("O tipo da tarefa não pode ser vazio.", nameof(taskType));

        var strategy = _strategies.FirstOrDefault(s =>
            string.Equals(s.TaskType, taskType.Trim(), StringComparison.OrdinalIgnoreCase));

        if (strategy is null)
        {
            var supportedTypes = string.Join(", ", _strategies.Select(s => $"'{s.TaskType}'"));
            throw new NotSupportedException(
                $"O tipo de tarefa '{taskType}' não possui um processador configurado. Tipos suportados: {supportedTypes}.");
        }

        return strategy;
    }
}
