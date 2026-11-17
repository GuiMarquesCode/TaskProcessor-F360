using F360.TaskProcessor.Domain.Entities;
using F360.TaskProcessor.Domain.Enums;
using F360.TaskProcessor.Domain.Interfaces;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace F360.TaskProcessor.Infrastructure.Persistence;

/// <summary>
/// Implementação do repositório utilizando MongoDB (NoSQL).
/// Inclui controle de concorrência atômico via FindOneAndUpdateAsync para múltiplos workers (Requisito 2.c).
/// </summary>
public class MongoTaskRepository : ITaskRepository
{
    private readonly IMongoCollection<TaskDocument> _collection;

    public MongoTaskRepository(IMongoDatabase database, IOptions<MongoDbSettings> settings)
    {
        _collection = database.GetCollection<TaskDocument>(settings.Value.TasksCollectionName);
    }

    public async Task CreateAsync(JobTask task, CancellationToken cancellationToken = default)
    {
        var document = TaskDocument.FromDomain(task);
        await _collection.InsertOneAsync(document, cancellationToken: cancellationToken);
    }

    public async Task<JobTask?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<TaskDocument>.Filter.Eq(d => d.Id, id);
        var document = await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
        return document?.ToDomain();
    }

    public async Task<IReadOnlyList<JobTask>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var documents = await _collection
            .Find(Builders<TaskDocument>.Filter.Empty)
            .SortByDescending(d => d.CreatedAt)
            .Limit(100)
            .ToListAsync(cancellationToken);

        return documents.Select(d => d.ToDomain()).ToList();
    }

    /// <summary>
    /// Controle de Concorrência no Banco NoSQL (Requisito 2.c):
    /// Executa uma operação atômica no MongoDB que só captura a tarefa se o status atual for 'Pendente'.
    /// No mesmo comando atômico, altera o status para 'EmProcessamento' e incrementa 'Attempts'.
    /// Caso outro worker tenha capturado a tarefa milissegundos antes, o filtro não encontrará o documento
    /// e o método retornará null sem gerar conflito.
    /// </summary>
    public async Task<JobTask?> TryAcquireForProcessingAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<TaskDocument>.Filter.And(
            Builders<TaskDocument>.Filter.Eq(d => d.Id, id),
            Builders<TaskDocument>.Filter.Eq(d => d.Status, TaskItemStatus.Pendente)
        );

        var update = Builders<TaskDocument>.Update
            .Set(d => d.Status, TaskItemStatus.EmProcessamento)
            .Inc(d => d.Attempts, 1)
            .Set(d => d.UpdatedAt, DateTime.UtcNow);

        var options = new FindOneAndUpdateOptions<TaskDocument>
        {
            ReturnDocument = ReturnDocument.After
        };

        var updatedDocument = await _collection.FindOneAndUpdateAsync(
            filter,
            update,
            options,
            cancellationToken);

        return updatedDocument?.ToDomain();
    }

    public async Task UpdateAsync(JobTask task, CancellationToken cancellationToken = default)
    {
        var document = TaskDocument.FromDomain(task);
        var filter = Builders<TaskDocument>.Filter.Eq(d => d.Id, task.Id);
        await _collection.ReplaceOneAsync(filter, document, cancellationToken: cancellationToken);
    }
}
