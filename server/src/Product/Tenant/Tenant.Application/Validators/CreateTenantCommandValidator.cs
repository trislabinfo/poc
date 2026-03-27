using FluentValidation;
using Tenant.Application.Commands.CreateTenant;

namespace Tenant.Application.Validators;

public sealed class CreateTenantCommandValidator : AbstractValidator<CreateTenantCommand>
{
    private static readonly System.Text.RegularExpressions.Regex SlugRegex = new(
        @"^[a-z0-9]+(?:-[a-z0-9]+)*$",
        System.Text.RegularExpressions.RegexOptions.Compiled
            | System.Text.RegularExpressions.RegexOptions.CultureInvariant);

    public CreateTenantCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200).WithMessage("Name cannot exceed 200 characters.");

        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Slug is required.")
            .MaximumLength(100).WithMessage("Slug cannot exceed 100 characters.")
            .Must(s => SlugRegex.IsMatch(s.Trim().ToLowerInvariant()))
            .WithMessage("Slug must contain only lowercase letters, numbers, and hyphens (e.g. my-tenant).");
    }
}
