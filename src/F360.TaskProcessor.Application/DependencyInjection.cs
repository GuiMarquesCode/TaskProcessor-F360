using F360.TaskProcessor.Application.Factories;
using F360.TaskProcessor.Application.Interfaces;
using F360.TaskProcessor.Application.Services;
using F360.TaskProcessor.Application.Strategies;
using Microsoft.Extensions.DependencyInjection;

namespace F360.TaskProcessor.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Serviços de Aplicação
        services.AddScoped<ITaskAppService, TaskAppService>();
        services.AddScoped<ITaskProcessingOrchestrator, TaskProcessingOrchestrator>();

        // Auxiliar de Composição para os Processadores (Composição sobre Herança)
        services.AddSingleton<ITaskSimulationHelper, TaskSimulationHelper>();

        // Registro das Estratégias de Processamento (Strategy Pattern + Open/Closed Principle)
        services.AddScoped<ITaskExecutionStrategy, SendEmailTaskStrategy>();
        services.AddScoped<ITaskExecutionStrategy, GenerateReportTaskStrategy>();

        // Fábrica de Estratégias (Factory Pattern)
        services.AddScoped<ITaskStrategyFactory, TaskStrategyFactory>();

        return services;
    }
}
