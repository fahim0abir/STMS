using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation.Results;

namespace SmartTask.Infrastructure.Exceptions;

public class FluentValidationException : Exception
{
    public IEnumerable<ValidationFailure> Failures { get; }

    public FluentValidationException(IEnumerable<ValidationFailure> failures)
    {
        Failures = failures;
    }
}
