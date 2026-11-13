using System;

namespace SmartTask.Infrastructure.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException() : base() { }
    public NotFoundException(string message) : base(message) { }
    public NotFoundException(string message, Exception innerException) : base(message, innerException) { }
    public NotFoundException(string? propertyName, object key) : base($"Entity not found: {propertyName} = {key}") { }
}
