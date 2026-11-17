using F360.TaskProcessor.Application.DTOs;

namespace F360.TaskProcessor.Application.Services;

/// <summary>
/// Interface do serviço de aplicação consumido pela API REST (Requisitos 1 e 3).
/// </summary>
public interface ITaskAppService
{
    Task<TaskResponse> CreateTaskAsync(CreateTaskRequest request, CancellationToken cancellationToken = default);
    Task<TaskResponse?> GetTaskByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TaskResponse>> GetAllTasksAsync(CancellationToken cancellationToken = default);
}
