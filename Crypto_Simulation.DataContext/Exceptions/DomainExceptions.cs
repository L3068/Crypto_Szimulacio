namespace Crypto_Simulation.DataContext.Exceptions
{
    /// <summary>
    /// Base type for errors that are caused by the caller and can safely be reported back to them.
    /// </summary>
    public abstract class DomainException : Exception
    {
        protected DomainException(string message) : base(message) { }
    }

    /// <summary>A requested resource does not exist. Maps to HTTP 404.</summary>
    public class NotFoundException : DomainException
    {
        public NotFoundException(string message) : base(message) { }

        public static NotFoundException For(string resource, object key) =>
            new NotFoundException($"{resource} with id '{key}' was not found.");
    }

    /// <summary>The request is well formed but violates a business rule. Maps to HTTP 400.</summary>
    public class ValidationException : DomainException
    {
        public ValidationException(string message) : base(message) { }
    }

    /// <summary>The request conflicts with the current state of the resource. Maps to HTTP 409.</summary>
    public class ConflictException : DomainException
    {
        public ConflictException(string message) : base(message) { }
    }

    /// <summary>The caller is authenticated but may not touch this resource. Maps to HTTP 403.</summary>
    public class ForbiddenException : DomainException
    {
        public ForbiddenException(string message) : base(message) { }
    }
}
