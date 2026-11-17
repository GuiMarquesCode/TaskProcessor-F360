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
        var service = new TaskAppService(
            repositoryMock.Object,
            publisherMock.Object,
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

        repositoryMock.Verify(
            r => r.CreateAsync(It.Is<JobTask>(t => t.Id == response.Id), It.IsAny<CancellationToken>()),
            Times.Once);

        publisherMock.Verify(
            p => p.PublishAsync(It.Is<TaskQueueMessage>(m => m.TaskId == response.Id), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
