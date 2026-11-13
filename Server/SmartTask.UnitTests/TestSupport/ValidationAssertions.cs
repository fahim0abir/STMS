using FluentValidation.TestHelper;
using Xunit;

namespace SmartTask.UnitTests.TestSupport;

/// <summary>
/// Assertion shims for FluentValidation's <see cref="TestValidationResult{T}"/> so the specs do
/// not depend on the exact set of extension methods shipped by a given FluentValidation version.
/// </summary>
internal static class ValidationAssertions
{
    public static void ShouldNotHaveAnyErrors<T>(this TestValidationResult<T> result)
    {
        Assert.True(
            result.IsValid,
            "Expected no validation errors but found: " +
            string.Join("; ", result.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}")));
    }

    public static void ShouldHaveAnyValidationError<T>(this TestValidationResult<T> result)
    {
        Assert.False(result.IsValid, "Expected validation errors but none were produced.");
    }
}
