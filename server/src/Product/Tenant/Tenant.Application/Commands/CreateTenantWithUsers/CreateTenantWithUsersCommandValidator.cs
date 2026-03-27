using FluentValidation;

namespace Tenant.Application.Commands.CreateTenantWithUsers;

public sealed class CreateTenantWithUsersCommandValidator : AbstractValidator<CreateTenantWithUsersCommand>
{
    private static readonly System.Text.RegularExpressions.Regex SlugRegex = new(
        @"^[a-z0-9]+(?:-[a-z0-9]+)*$",
        System.Text.RegularExpressions.RegexOptions.Compiled
            | System.Text.RegularExpressions.RegexOptions.CultureInvariant);

    public CreateTenantWithUsersCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200).WithMessage("Name cannot exceed 200 characters.");

        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Slug is required.")
            .MaximumLength(100).WithMessage("Slug cannot exceed 100 characters.")
            .Must(s => SlugRegex.IsMatch(s.Trim().ToLowerInvariant()))
            .WithMessage("Slug must contain only lowercase letters, numbers, and hyphens.");

        RuleFor(x => x.Users)
            .NotEmpty().WithMessage("At least one user is required.")
            .Must(users => users.Any(u => u.IsTenantOwner))
            .WithMessage("At least one tenant owner is required.");

        RuleForEach(x => x.Users).ChildRules(user =>
        {
            user.RuleFor(u => u.Email)
                .NotEmpty().WithMessage("User email is required.")
                .EmailAddress().WithMessage("Invalid email format.");
            user.RuleFor(u => u.DisplayName)
                .NotEmpty().WithMessage("User display name is required.")
                .MaximumLength(200).WithMessage("Display name cannot exceed 200 characters.");
            user.RuleFor(u => u.Password)
                .NotEmpty().WithMessage("Password is required for each user.");
        });
    }
}
