using F360.TaskProcessor.Domain.Entities;
using F360.TaskProcessor.Domain.Enums;
using F360.TaskProcessor.Domain.Exceptions;
using Xunit;

namespace F360.TaskProcessor.UnitTests.Domain;

public class JobTaskTests
{
    [Fact]
    public void Constructor_WithValidData_ShouldInitializeWithPendingStatusAndZeroAttempts()
    {
        // Arrange & Act
        var task = new JobTask("EnviarEmail", "{\"destinatario\":\"cliente@f360.com.br\"}", maxAttempts: 3);

        // Assert
        Assert.NotEqual(Guid.Empty, task.Id);
        Assert.Equal("EnviarEmail", task.Type);
        Assert.Equal(TaskItemStatus.Pendente, task.Status);
        Assert.Equal(0, task.Attempts);
        Assert.Equal(3, task.MaxAttempts);
        Assert.True(task.CanRetry());
        Assert.Null(task.LastError);
        Assert.Null(task.CompletedAt);
    }

    [Theory]
    [InlineData("", "{\"ok\":true}", 3)]
    [InlineData("   ", "{\"ok\":true}", 3)]
    [InlineData("EnviarEmail", "", 3)]
    [InlineData("EnviarEmail", "{\"ok\":true}", 0)]
    [InlineData("EnviarEmail", "{\"ok\":true}", -1)]
    public void Constructor_WithInvalidArguments_ShouldThrowDomainException(string type, string dataJson, int maxAttempts)
    {
        // Act & Assert
        Assert.Throws<DomainException>(() => new JobTask(type, dataJson, maxAttempts));
    }

    [Fact]
    public void MarkAsProcessing_ShouldChangeStatusToEmProcessamentoAndIncrementAttempts()
    {
        // Arrange
        var task = new JobTask("GerarRelatorio", "{\"mes\":11}");

        // Act
        task.MarkAsProcessing();

        // Assert
        Assert.Equal(TaskItemStatus.EmProcessamento, task.Status);
        Assert.Equal(1, task.Attempts);
    }

    [Fact]
    public void MarkAsCompleted_WhenProcessing_ShouldChangeStatusToConcluido()
    {
        // Arrange
        var task = new JobTask("GerarRelatorio", "{\"mes\":11}");
        task.MarkAsProcessing();

        // Act
        task.MarkAsCompleted();

        // Assert
        Assert.Equal(TaskItemStatus.Concluido, task.Status);
        Assert.NotNull(task.CompletedAt);
        Assert.Null(task.LastError);
    }

    [Fact]
    public void RegisterFailure_WhenAttemptsBelowMax_ShouldReturnStatusToPendenteForRetry()
    {
        // Arrange
        var task = new JobTask("EnviarEmail", "{\"destinatario\":\"teste@f360.com.br\"}", maxAttempts: 3);
        task.MarkAsProcessing(); // Tentativa 1 de 3

        // Act
        task.RegisterFailure("Timeout no servidor SMTP");

        // Assert
        Assert.Equal(TaskItemStatus.Pendente, task.Status);
        Assert.Equal("Timeout no servidor SMTP", task.LastError);
        Assert.True(task.CanRetry());
    }

    [Fact]
    public void RegisterFailure_WhenMaxAttemptsReached_ShouldChangeStatusToErroPermanently()
    {
        // Arrange
        var task = new JobTask("EnviarEmail", "{\"destinatario\":\"teste@f360.com.br\"}", maxAttempts: 2);

        // Tentativa 1 falha
        task.MarkAsProcessing();
        task.RegisterFailure("Falha 1");
        Assert.Equal(TaskItemStatus.Pendente, task.Status);

        // Tentativa 2 (última) falha
        task.MarkAsProcessing();
        task.RegisterFailure("Falha definitiva na tentativa 2");

        // Assert
        Assert.Equal(2, task.Attempts);
        Assert.False(task.CanRetry());
        Assert.Equal(TaskItemStatus.Erro, task.Status);
        Assert.Equal("Falha definitiva na tentativa 2", task.LastError);
    }
}
