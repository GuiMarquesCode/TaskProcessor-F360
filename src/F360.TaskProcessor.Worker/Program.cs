using F360.TaskProcessor.Application;
using F360.TaskProcessor.Infrastructure;
using F360.TaskProcessor.Worker.Workers;

var builder = Host.CreateApplicationBuilder(args);

// Registro das camadas da Clean Architecture
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Registro do Worker em Background (Requisito 2)
builder.Services.AddHostedService<RabbitMqTaskConsumerWorker>();

var host = builder.Build();
host.Run();
