namespace F360.TaskProcessor.Application.Interfaces;

/// <summary>
/// Contrato do Factory Pattern para resolver polimorficamente qual estratégia
/// deve processar um determinado tipo de tarefa (Open/Closed Principle).
/// </summary>
public interface ITaskStrategyFactory
{
    ITaskExecutionStrategy GetStrategy(string taskType);
}
