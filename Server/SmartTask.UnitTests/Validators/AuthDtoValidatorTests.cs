using FluentValidation.TestHelper;
using SmartTask.Application.DTOs.Auth;
using SmartTask.Application.Validators;
using SmartTask.UnitTests.TestSupport;

namespace SmartTask.UnitTests.Validators;

public class AuthDtoValidatorTests
{
    private readonly RegisterDtoValidator _registerValidator = new();
    private readonly LoginDtoValidator _loginValidator = new();

    [Fact]
    public void Register_WithValidPayload_HasNoErrors()
    {
        var result = _registerValidator.TestValidate(new RegisterDto
        {
            Email = "user@example.com",
            Password = "Password123",
            Name = "Jane Doe"
        });

        result.ShouldNotHaveAnyErrors();
    }

    [Theory]
    [InlineData("", "Password123")]
    [InlineData("not-an-email", "Password123")]
    [InlineData("user@example.com", "short")]
    public void Register_WithInvalidCredentials_ReportsError(string email, string password)
    {
        var result = _registerValidator.TestValidate(new RegisterDto { Email = email, Password = password });

        result.ShouldHaveAnyValidationError();
    }

    [Fact]
    public void Register_WithNameLongerThan100Characters_ReportsError()
    {
        var result = _registerValidator.TestValidate(new RegisterDto
        {
            Email = "user@example.com",
            Password = "Password123",
            Name = new string('a', 101)
        });

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Login_WithValidPayload_HasNoErrors()
    {
        var result = _loginValidator.TestValidate(new LoginDto
        {
            Email = "user@example.com",
            Password = "secret1"
        });

        result.ShouldNotHaveAnyErrors();
    }

    [Fact]
    public void Login_WithPasswordShorterThan6Characters_ReportsError()
    {
        var result = _loginValidator.TestValidate(new LoginDto { Email = "user@example.com", Password = "12345" });

        result.ShouldHaveValidationErrorFor(x => x.Password);
    }
}
