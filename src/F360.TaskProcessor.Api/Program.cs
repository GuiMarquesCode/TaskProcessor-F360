using System.Reflection;
using F360.TaskProcessor.Application;
using F360.TaskProcessor.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

// Registro das camadas da Clean Architecture
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Swagger habilitado em todos os ambientes (inclusive Docker) para facilitar a avaliação técnica
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "F360 Task Processor API v1");
});

app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

app.MapControllers();

app.Run();
