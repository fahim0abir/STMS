using FluentValidation.TestHelper;
using SmartTask.Application.DTOs;
using SmartTask.Application.Validators;
using SmartTask.UnitTests.TestSupport;
using TaskStatus = SmartTask.Domain.Enums.TaskStatus;

namespace SmartTask.UnitTests.Validators;

public class ProjectQueryDtoValidatorTests
{
    private readonly ProjectQueryDtoValidator _validator = new();

    [Fact]
    public void Defaults_AreValid()
    {
        _validator.TestValidate(new ProjectQueryDto()).ShouldNotHaveAnyErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void PageNumberBelowOne_ReportsError(int pageNumber)
    {
        var result = _validator.TestValidate(new ProjectQueryDto { PageNumber = pageNumber });

        result.ShouldHaveValidationErrorFor(x => x.PageNumber);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void PageSizeOutsideRange_ReportsError(int pageSize)
    {
        var result = _validator.TestValidate(new ProjectQueryDto { PageSize = pageSize });

        result.ShouldHaveValidationErrorFor(x => x.PageSize);
    }

    [Theory]
    [InlineData("asc")]
    [InlineData("DESC")]
    [InlineData("")]
    public void ValidSortOrder_HasNoErrors(string sortOrder)
    {
        var result = _validator.TestValidate(new ProjectQueryDto { SortOrder = sortOrder });

        result.ShouldNotHaveValidationErrorFor(x => x.SortOrder);
    }

    [Fact]
    public void InvalidSortOrder_ReportsError()
    {
        var result = _validator.TestValidate(new ProjectQueryDto { SortOrder = "sideways" });

        result.ShouldHaveValidationErrorFor(x => x.SortOrder);
    }

    [Theory]
    [InlineData("Name")]
    [InlineData("CreatedAt")]
    [InlineData(null)]
    public void AllowedSortBy_HasNoErrors(string? sortBy)
    {
        var result = _validator.TestValidate(new ProjectQueryDto { SortBy = sortBy });

        result.ShouldNotHaveValidationErrorFor(x => x.SortBy);
    }

    [Fact]
    public void UnknownSortBy_ReportsError()
    {
        var result = _validator.TestValidate(new ProjectQueryDto { SortBy = "OwnerEmail" });

        result.ShouldHaveValidationErrorFor(x => x.SortBy);
    }
}

public class TaskItemQueryDtoValidatorTests
{
    private readonly TaskItemQueryDtoValidator _validator = new();

    [Fact]
    public void Defaults_AreValid()
    {
        _validator.TestValidate(new TaskItemQueryDto()).ShouldNotHaveAnyErrors();
    }

    [Fact]
    public void PageSizeAboveHundred_ReportsError()
    {
        var result = _validator.TestValidate(new TaskItemQueryDto { PageSize = 250 });

        result.ShouldHaveValidationErrorFor(x => x.PageSize);
    }

    [Fact]
    public void DueToBeforeDueFrom_ReportsError()
    {
        var result = _validator.TestValidate(new TaskItemQueryDto
        {
            DueFrom = new DateTime(2026, 5, 10, 0, 0, 0, DateTimeKind.Utc),
            DueTo = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc)
        });

        result.ShouldHaveValidationErrorFor(x => x.DueTo);
    }

    [Fact]
    public void DueRangeInCorrectOrder_HasNoErrors()
    {
        var result = _validator.TestValidate(new TaskItemQueryDto
        {
            DueFrom = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            DueTo = new DateTime(2026, 5, 10, 0, 0, 0, DateTimeKind.Utc),
            Status = TaskStatus.InProgress,
            SortBy = "Priority",
            SortOrder = "asc"
        });

        result.ShouldNotHaveAnyErrors();
    }

    [Fact]
    public void UnknownSortField_ReportsError()
    {
        var result = _validator.TestValidate(new TaskItemQueryDto { SortBy = "AssigneeEmail" });

        result.ShouldHaveValidationErrorFor(x => x.SortBy);
    }
}
