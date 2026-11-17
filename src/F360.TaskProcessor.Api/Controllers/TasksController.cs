using F360.TaskProcessor.Application.DTOs;
using F360.TaskProcessor.Application.Services;
using F360.TaskProcessor.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace F360.TaskProcessor.Api.Controllers;

[ApiController]
[Route("api/tasks")]
[Produces("application/json")]
public class TasksController : ControllerBase
{
    private readonly ITaskAppService _taskAppService;

    public TasksController(ITaskAppService taskAppService)
    {
        _taskAppService = taskAppService;
    }

    /// <summary>
    /// Cria uma nova tarefa (job) para processamento assíncrono em background (Requisito 1).
    /// </summary>
    /// <remarks>
    /// Exemplos de tipos suportados: "EnviarEmail", "GerarRelatorio".
    /// Para simular falha e testar re-tentativas na apresentação, envie {"simularFalha": true} dentro de "dados".
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateTaskRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var createdTask = await _taskAppService.CreateTaskAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = createdTask.Id }, createdTask);
        }
        catch (Exception ex) when (ex is DomainException or NotSupportedException)
        {
            return BadRequest(new { erro = ex.Message });
        }
    }

    /// <summary>
    /// Consulta o status atual e detalhes de uma tarefa pelo seu Id (Requisito 3.b).
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var task = await _taskAppService.GetTaskByIdAsync(id, cancellationToken);
        if (task is null)
        {
            return NotFound(new { mensagem = $"Tarefa com Id '{id}' não encontrada." });
        }

        return Ok(task);
    }

    /// <summary>
    /// Lista as tarefas cadastradas ordenadas da mais recente para a mais antiga.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TaskResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var tasks = await _taskAppService.GetAllTasksAsync(cancellationToken);
        return Ok(tasks);
    }
}
