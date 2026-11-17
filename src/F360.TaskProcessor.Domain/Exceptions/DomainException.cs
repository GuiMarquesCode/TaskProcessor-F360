namespace F360.TaskProcessor.Domain.Exceptions;

/// <summary>
/// Exceção lançada quando uma regra de negócio ou invariante da entidade de domínio é violada.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }
}
