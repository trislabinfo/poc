using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Domain;
using BuildingBlocks.Kernel.Results;
using Identity.Domain.Entities;
using Identity.Domain.Repositories;
using Identity.Domain.ValueObjects;

namespace Identity.Application.Commands.Users.CreateUser;

public sealed class CreateUserCommandHandler : IApplicationRequestHandler<CreateUserCommand, Result<Guid>>
{
    private readonly IUserRepository _userRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateUserCommandHandler(
        IUserRepository userRepository,
        IDateTimeProvider dateTimeProvider)
    {
        _userRepository = userRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> HandleAsync(
        CreateUserCommand request,
        CancellationToken cancellationToken)
    {
        var emailResult = Email.Create(request.Email);
        if (emailResult.IsFailure)
        {
            return Result<Guid>.Failure(emailResult.Error);
        }

        var existingUser = await _userRepository.GetByEmailAsync(
            emailResult.Value,
            cancellationToken);

        if (existingUser is not null)
        {
            if (existingUser.DefaultTenantId == request.DefaultTenantId)
                return Result<Guid>.Success(existingUser.Id);
            return Result<Guid>.Failure(
                Error.Conflict("Identity.User.EmailAlreadyExists", "User with this email already exists."));
        }

        var userResult = User.Create(
            request.DefaultTenantId,
            emailResult.Value,
            request.DisplayName,
            _dateTimeProvider);

        if (userResult.IsFailure)
        {
            return Result<Guid>.Failure(userResult.Error);
        }

        var user = userResult.Value;
        await _userRepository.AddAsync(user, cancellationToken);

        // TODO Phase 2: Hash and store password when User has PasswordHash property.
        _ = request.Password;

        return Result<Guid>.Success(user.Id);
    }
}
