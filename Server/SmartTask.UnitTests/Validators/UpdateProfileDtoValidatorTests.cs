using FluentValidation.TestHelper;
using SmartTask.Application.DTOs;
using SmartTask.Application.Validators;

namespace SmartTask.UnitTests.Validators;

public class UpdateProfileDtoValidatorTests
{
    private readonly UpdateProfileDtoValidator _validator = new();

    [Fact]
    public void WithoutPasswordChange_HasNoErrors()
    {
        var result = _validator.TestValidate(new UpdateProfileDto
        {
            Email = "user@example.com",
            Name = "Renamed User"
        });

        Assert.True(result.IsValid, string.Join("; ", result.Errors.Select(e => e.ErrorMessage)));
    }

    [Fact]
    public void MatchingConfirmation_HasNoErrors()
    {
        var result = _validator.TestValidate(new UpdateProfileDto
        {
            Email = "user@example.com",
            CurrentPassword = "OldPassw0rd!",
            NewPassword = "NewPassw0rd!",
            ConfirmPassword = "NewPassw0rd!"
        });

        Assert.True(result.IsValid, string.Join("; ", result.Errors.Select(e => e.ErrorMessage)));
    }

    [Fact]
    public void MissingConfirmation_ReportsError()
    {
        var result = _validator.TestValidate(new UpdateProfileDto
        {
            Email = "user@example.com",
            CurrentPassword = "OldPassw0rd!",
            NewPassword = "NewPassw0rd!"
        });

        result.ShouldHaveValidationErrorFor(x => x.ConfirmPassword);
    }

    [Fact]
    public void MismatchedConfirmation_ReportsError()
    {
        var result = _validator.TestValidate(new UpdateProfileDto
        {
            Email = "user@example.com",
            CurrentPassword = "OldPassw0rd!",
            NewPassword = "NewPassw0rd!",
            ConfirmPassword = "OtherPassw0rd!"
        });

        result.ShouldHaveValidationErrorFor(x => x.ConfirmPassword)
            .WithErrorMessage("Passwords do not match.");
    }

    [Fact]
    public void NewPassword_WithoutCurrentPassword_ReportsError()
    {
        var result = _validator.TestValidate(new UpdateProfileDto
        {
            Email = "user@example.com",
            NewPassword = "NewPassw0rd!",
            ConfirmPassword = "NewPassw0rd!"
        });

        result.ShouldHaveValidationErrorFor(x => x.CurrentPassword);
    }
}