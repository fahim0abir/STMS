using FluentValidation.TestHelper;
using SmartTask.Application.DTOs;
using SmartTask.Application.Validators;
using SmartTask.UnitTests.TestSupport;
using TaskPriority = SmartTask.Domain.Enums.TaskPriority;
using TaskStatus = SmartTask.Domain.Enums.TaskStatus;

namespace SmartTask.UnitTests.Validators;

public class ProjectCreateDtoValidatorTests
{
    private readonly ProjectCreateDtoValidator _validator = new();

    [Fact]
    public void Valid_Project_HasNoErrors()
    {
        var result = _validator.TestValidate(new ProjectCreateDto
        {
            Name = "Website revamp",
            Description = "Q3 work",
            TechStack = "ASP.NET Core 9, Angular 18",
            StartDate = DateTime.UtcNow
        });

        result.ShouldNotHaveAnyErrors();
    }

    [Fact]
    public void EmptyName_ReportsError()
    {
        var result = _validator.TestValidate(new ProjectCreateDto { Name = string.Empty });

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void NameLongerThan200Characters_ReportsError()
    {
        var result = _validator.TestValidate(new ProjectCreateDto { Name = new string('a', 201) });

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void DescriptionLongerThan500Characters_ReportsError()
    {
        var result = _validator.TestValidate(new ProjectCreateDto
        {
            Name = "Valid",
            Description = new string('d', 501)
        });

        result.ShouldHaveValidationErrorFor(x => x.Description);
    }
}

public class TaskItemCreateDtoValidatorTests
{
    private readonly TaskItemCreateDtoValidator _validator = new();

    [Fact]
    public void Valid_Task_HasNoErrors()
    {
        var result = _validator.TestValidate(new TaskItemCreateDto
        {
            Title = "Write tests",
            Description = "Cover services",
            Status = TaskStatus.InProgress,
            Priority = TaskPriority.High,
            DueAt = DateTime.UtcNow.AddDays(2)
        });

        result.ShouldNotHaveAnyErrors();
    }

    [Fact]
    public void EmptyTitle_ReportsError()
    {
        var result = _validator.TestValidate(new TaskItemCreateDto { Title = string.Empty });

        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void TitleLongerThan200Characters_ReportsError()
    {
        var result = _validator.TestValidate(new TaskItemCreateDto { Title = new string('t', 201) });

        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void DescriptionLongerThan4000Characters_ReportsError()
    {
        var result = _validator.TestValidate(new TaskItemCreateDto
        {
            Title = "Valid",
            Description = new string('d', 4001)
        });

        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void StatusOutsideEnum_ReportsError()
    {
        var result = _validator.TestValidate(new TaskItemCreateDto { Title = "Valid", Status = (TaskStatus)99 });

        result.ShouldHaveValidationErrorFor(x => x.Status);
    }

    [Fact]
    public void PriorityOutsideEnum_ReportsError()
    {
        var result = _validator.TestValidate(new TaskItemCreateDto { Title = "Valid", Priority = (TaskPriority)42 });

        result.ShouldHaveValidationErrorFor(x => x.Priority);
    }

    [Fact]
    public void DueDateOlderThanOneYear_ReportsError()
    {
        var result = _validator.TestValidate(new TaskItemCreateDto
        {
            Title = "Valid",
            DueAt = DateTime.UtcNow.AddYears(-2)
        });

        result.ShouldHaveValidationErrorFor(x => x.DueAt);
    }
}

public class EnhanceDescriptionRequestDtoValidatorTests
{
    private readonly EnhanceDescriptionRequestDtoValidator _validator = new();

    [Fact]
    public void Valid_Request_HasNoErrors()
    {
        var result = _validator.TestValidate(new EnhanceDescriptionRequestDto
        {
            Description = "Implement JWT refresh token rotation for the API.",
            Title = "Auth",
            Context = "Backend"
        });

        result.ShouldNotHaveAnyErrors();
    }

    [Fact]
    public void DescriptionShorterThan20Characters_ReportsError()
    {
        var result = _validator.TestValidate(new EnhanceDescriptionRequestDto { Description = "too short" });

        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void DescriptionLongerThan4000Characters_ReportsError()
    {
        var result = _validator.TestValidate(new EnhanceDescriptionRequestDto { Description = new string('x', 4001) });

        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void TitleLongerThan200Characters_ReportsError()
    {
        var result = _validator.TestValidate(new EnhanceDescriptionRequestDto
        {
            Description = new string('x', 25),
            Title = new string('t', 201)
        });

        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void EmptyOptionalTitleAndContext_AreAllowed()
    {
        var result = _validator.TestValidate(new EnhanceDescriptionRequestDto
        {
            Description = new string('x', 25),
            Title = "   ",
            Context = null
        });

        result.ShouldNotHaveAnyErrors();
    }
}
