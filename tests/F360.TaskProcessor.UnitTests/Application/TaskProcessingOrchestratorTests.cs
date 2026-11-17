using F360.TaskProcessor.Application.DTOs;
using F360.TaskProcessor.Application.Interfaces;
using F360.TaskProcessor.Application.Services;
using F360.TaskProcessor.Domain.Entities;
using F360.TaskProcessor.Domain.Enums;
using F360.TaskProcessor.Domain.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace F360.TaskProcessor.UnitTests.Application;

public class TaskProcessingOrchestratorTests
{
    private readonly Mock<ITaskRepository> _repositoryMock = new();
    private readonly Mock<ITaskStrategyFactory> _factoryMock = new();
    private readonly Mock<IMessageQueuePublisher> _publisherMock = new();
    private readonly Mock<ITaskExecutionStrategy> _strategyMock = new();
    private readonly TaskProcessingOrchestrator _orchestrator;

    public TaskProcessingOrchestratorTests()
    {
        _orchestrator = new TaskProcessingOrchestrator(
            _repositoryMock.Object,
            _factoryMock.Object,
            _publisherMock.Object,
            NullLogger<TaskProcessingOrchestrator>.Instance);
    }

    [Fact]
    public async Task ProcessTaskAsync_WhenConcurrencyLockReturnsNull_ShouldIgnoreAndNotExecuteStrategy()
    {
        // Arrange: simula que outro worker já capturou a tarefa simultaneamente
        var taskId = Guid.NewGuid();
        _repositoryMock
            .Setup(r => r.TryAcquireForProcessingAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((JobTask?)null);

        // Act
        await _orchestrator.ProcessTaskAsync(taskId);

        // Assert
        _factoryMock.Verify(f => f.GetStrategy(It.IsAny<string>()), Times.Never);
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<JobTask>(), It.IsAny<CancellationToken>()), Times.Never);
        _publisherMock.Verify(p => p.PublishAsync(It.IsAny<TaskQueueMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessTaskAsync_WhenStrategySucceeds_ShouldMarkTaskAsConcluido()
    {
        // Arrange
        var acquiredTask = new JobTask(
            id: Guid.NewGuid(),
            type: "EnviarEmail",
            dataJson: "{\"destinatario\":\"cliente@f360.com.br\"}",
            status: TaskItemStatus.EmProcessamento,
            attempts: 1,
            maxAttempts: 3,
            lastError: null,
            createdAt: DateTime.UtcNow,
            updatedAt: DateTime.UtcNow,
            completedAt: null);

        _repositoryMock
            .Setup(r => r.TryAcquireForProcessingAsync(acquiredTask.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(acquiredTask);

        _factoryMock
            .Setup(f => f.GetStrategy("EnviarEmail"))
            .Returns(_strategyMock.Object);

        // Act
        await _orchestrator.ProcessTaskAsync(acquiredTask.Id);

        // Assert
        Assert.Equal(TaskItemStatus.Concluido, acquiredTask.Status);
        _repositoryMock.Verify(r => r.UpdateAsync(acquiredTask, It.IsAny<CancellationToken>()), Times.Once);
        _publisherMock.Verify(p => p.PublishAsync(It.IsAny<TaskQueueMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessTaskAsync_WhenStrategyFailsAndHasRetriesLeft_ShouldRequeueAndSetStatusPendente()
    {
        // Arrange: tentativa 1 de 3
        var acquiredTask = new JobTask(
            id: Guid.NewGuid(),
            type: "GerarRelatorio",
            dataJson: "{\"simularFalha\":true}",
            status: TaskItemStatus.EmProcessamento,
            attempts: 1,
            maxAttempts: 3,
            lastError: null,
            createdAt: DateTime.UtcNow,
            updatedAt: DateTime.UtcNow,
            completedAt: null);

        _repositoryMock
            .Setup(r => r.TryAcquireForProcessingAsync(acquiredTask.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(acquiredTask);

        _factoryMock
            .Setup(f => f.GetStrategy("GerarRelatorio"))
            .Returns(_strategyMock.Object);

        _strategyMock
            .Setup(s => s.ExecuteAsync(acquiredTask, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Erro temporário ao gerar relatório"));

        // Act
        await _orchestrator.ProcessTaskAsync(acquiredTask.Id);

        // Assert: volta para Pendente e re-publica na fila para nova tentativa
        Assert.Equal(TaskItemStatus.Pendente, acquiredTask.Status);
        Assert.Equal("Erro temporário ao gerar relatório", acquiredTask.LastError);
        _repositoryMock.Verify(r => r.UpdateAsync(acquiredTask, It.IsAny<CancellationToken>()), Times.Once);
        _publisherMock.Verify(
            p => p.PublishAsync(
                It.Is<TaskQueueMessage>(m => m.TaskId == acquiredTask.Id),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessTaskAsync_WhenStrategyFailsOnFinalAttempt_ShouldSetStatusErroAndNotRequeue()
    {
        // Arrange: tentativa 3 de 3 (última tentativa)
        var acquiredTask = new JobTask(
            id: Guid.NewGuid(),
            type: "GerarRelatorio",
            dataJson: "{\"simularFalha\":true}",
            status: TaskItemStatus.EmProcessamento,
            attempts: 3,
            maxAttempts: 3,
            lastError: null,
            createdAt: DateTime.UtcNow,
            updatedAt: DateTime.UtcNow,
            completedAt: null);

        _repositoryMock
            .Setup(r => r.TryAcquireForProcessingAsync(acquiredTask.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(acquiredTask);

        _factoryMock
            .Setup(f => f.GetStrategy("GerarRelatorio"))
            .Returns(_strategyMock.Object);

        _strategyMock
            .Setup(s => s.ExecuteAsync(acquiredTask, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Falha na última tentativa"));

        // Act
        await _orchestrator.ProcessTaskAsync(acquiredTask.Id);

        // Assert: muda definitivamente para Erro e NÃO re-enfileira
        Assert.Equal(TaskItemStatus.Erro, acquiredTask.Status);
        Assert.Equal("Falha na última tentativa", acquiredTask.LastError);
        _repositoryMock.Verify(r => r.UpdateAsync(acquiredTask, It.IsAny<CancellationToken>()), Times.Once);
        _publisherMock.Verify(p => p.PublishAsync(It.IsAny<TaskQueueMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
