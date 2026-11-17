using F360.TaskProcessor.Application.Factories;
using F360.TaskProcessor.Application.Interfaces;
using F360.TaskProcessor.Application.Strategies;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace F360.TaskProcessor.UnitTests.Application;

public class TaskStrategyFactoryTests
{
    private readonly TaskStrategyFactory _factory;

    public TaskStrategyFactoryTests()
    {
        var simulationMock = new Mock<ITaskSimulationHelper>();
        var strategies = new ITaskExecutionStrategy[]
        {
            new SendEmailTaskStrategy(simulationMock.Object, NullLogger<SendEmailTaskStrategy>.Instance),
            new GenerateReportTaskStrategy(simulationMock.Object, NullLogger<GenerateReportTaskStrategy>.Instance)
        };

        _factory = new TaskStrategyFactory(strategies);
    }

    [Theory]
    [InlineData("EnviarEmail", typeof(SendEmailTaskStrategy))]
    [InlineData("enviaremail", typeof(SendEmailTaskStrategy))]
    [InlineData("GerarRelatorio", typeof(GenerateReportTaskStrategy))]
    [InlineData("GERARRELATORIO", typeof(GenerateReportTaskStrategy))]
    public void GetStrategy_WithSupportedType_ShouldReturnCorrectStrategyImplementation(string taskType, Type expectedType)
    {
        // Act
        var strategy = _factory.GetStrategy(taskType);

        // Assert
        Assert.NotNull(strategy);
        Assert.IsType(expectedType, strategy);
    }

    [Fact]
    public void GetStrategy_WithUnsupportedType_ShouldThrowNotSupportedException()
    {
        // Act & Assert
        Assert.Throws<NotSupportedException>(() => _factory.GetStrategy("TipoInexistente"));
    }
}
