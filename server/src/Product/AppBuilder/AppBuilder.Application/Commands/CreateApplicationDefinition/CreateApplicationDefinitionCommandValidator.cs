using FluentValidation;

namespace AppBuilder.Application.Commands.CreateAppDefinition;

public sealed class CreateAppDefinitionCommandValidator : AbstractValidator<CreateAppDefinitionCommand>
{
    public CreateAppDefinitionCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200).WithMessage("Name cannot exceed 200 characters.");
        RuleFor(x => x.Description)
            .NotNull().WithMessage("Description is required.")
            .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters.");
        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Slug is required.")
            .MaximumLength(100).WithMessage("Slug cannot exceed 100 characters.")
            .Matches(@"^[a-z0-9]+(?:-[a-z0-9]+)*$").WithMessage("Slug must contain only lowercase letters, numbers, and hyphens.");
    }
}
