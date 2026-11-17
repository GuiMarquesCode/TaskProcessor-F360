namespace F360.TaskProcessor.Infrastructure.Persistence;

public class MongoDbSettings
{
    public const string SectionName = "MongoDb";

    public string ConnectionString { get; set; } = "mongodb://localhost:27017";
    public string DatabaseName { get; set; } = "F360TaskProcessorDb";
    public string TasksCollectionName { get; set; } = "tasks";
}
