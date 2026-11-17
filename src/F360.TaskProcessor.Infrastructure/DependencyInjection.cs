using F360.TaskProcessor.Application.Interfaces;
using F360.TaskProcessor.Domain.Interfaces;
using F360.TaskProcessor.Infrastructure.Messaging;
using F360.TaskProcessor.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace F360.TaskProcessor.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configurações tipadas (Options Pattern)
        services.Configure<MongoDbSettings>(configuration.GetSection(MongoDbSettings.SectionName));
        services.Configure<RabbitMqSettings>(configuration.GetSection(RabbitMqSettings.SectionName));

        // Registro do cliente MongoDB (Singleton conforme recomendação oficial do MongoDB.Driver)
        services.AddSingleton<IMongoClient>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<MongoDbSettings>>().Value;
            return new MongoClient(settings.ConnectionString);
        });

        services.AddScoped<IMongoDatabase>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<MongoDbSettings>>().Value;
            var client = sp.GetRequiredService<IMongoClient>();
            return client.GetDatabase(settings.DatabaseName);
        });

        // Repositório NoSQL
        services.AddScoped<ITaskRepository, MongoTaskRepository>();

        // Publicador RabbitMQ
        services.AddSingleton<IMessageQueuePublisher, RabbitMqPublisher>();

        return services;
    }
}
