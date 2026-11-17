using System.Text.Json;
using F360.TaskProcessor.Application.DTOs;
using F360.TaskProcessor.Application.Interfaces;
using F360.TaskProcessor.Application.Services;
using F360.TaskProcessor.Domain.Entities;
using F360.TaskProcessor.Domain.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace F360.TaskProcessor.UnitTests.Application;

public class TaskAppServiceTests
{
    [Fact]
    public async Task CreateTaskAsync_WithValidRequest_ShouldPersistInRepositoryAndPublishToQueue()
    {
        // Arrange
        var repositoryMock = new Mock<ITaskRepository>();
        var publisherMock = new Mock<IMessageQueuePublisher>();
        var strategyFactoryMock = new Mock<ITaskStrategyFactory>();
        var service = new TaskAppService(
            repositoryMock.Object,
            publisherMock.Object,
            strategyFactoryMock.Object,
            NullLogger<TaskAppService>.Instance);

        using var jsonDoc = JsonDocument.Parse("{\"destinatario\":\"financeiro@f360.com.br\"}");
        var request = new CreateTaskRequest
        {
            Tipo = "EnviarEmail",
            Dados = jsonDoc.RootElement.Clone(),
            MaxTentativas = 3
        };

        // Act
        var response = await service.CreateTaskAsync(request);

        // Assert
        Assert.NotEqual(Guid.Empty, response.Id);
        Assert.Equal("EnviarEmail", response.Tipo);
        Assert.Equal("Pendente", response.Status);
        Assert.Equal(0, response.TentativasRealizadas);
        Assert.Equal(3, response.MaximoTentativas);

        strategyFactoryMock.Verify(f => f.GetStrategy("EnviarEmail"), Times.Once);

        repositoryMock.Verify(
            r => r.CreateAsync(It.Is<JobTask>(t => t.Id == response.Id), It.IsAny<CancellationToken>()),
            Times.Once);

        publisherMock.Verify(
            p => p.PublishAsync(It.Is<TaskQueueMessage>(m => m.TaskId == response.Id), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateTaskAsync_WithUnsupportedType_ShouldThrowNotSupportedExceptionAndNotPersistOrPublish()
    {
        // Arrange
        var repositoryMock = new Mock<ITaskRepository>();
        var publisherMock = new Mock<IMessageQueuePublisher>();
        var strategyFactoryMock = new Mock<ITaskStrategyFactory>();

        strategyFactoryMock
            .Setup(f => f.GetStrategy("TipoInvalido"))
            .Throws(new NotSupportedException("Tipo não suportado."));

        var service = new TaskAppService(
            repositoryMock.Object,
            publisherMock.Object,
            strategyFactoryMock.Object,
            NullLogger<TaskAppService>.Instance);

        using var jsonDoc = JsonDocument.Parse("{\"teste\":true}");
        var request = new CreateTaskRequest
        {
            Tipo = "TipoInvalido",
            Dados = jsonDoc.RootElement.Clone(),
            MaxTentativas = 3
        };

        // Act & Assert
        await Assert.ThrowsAsync<NotSupportedException>(() => service.CreateTaskAsync(request));

        repositoryMock.Verify(
            r => r.CreateAsync(It.IsAny<JobTask>(), It.IsAny<CancellationToken>()),
            Times.Never);

        publisherMock.Verify(
            p => p.PublishAsync(It.IsAny<TaskQueueMessage>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
