using CoreAxis.Modules.MLMModule.Application.DTOs;
using MediatR;

namespace CoreAxis.Modules.MLMModule.Application.Commands;

public class CreateUserReferralCommand : IRequest<UserReferralDto>
{
    public Guid UserId { get; set; }
    public Guid? ParentUserId { get; set; }
}

public class UpdateUserReferralCommand : IRequest<UserReferralDto>
{
    public Guid Id { get; set; }
    public Guid? ParentUserId { get; set; }
}

public class ActivateUserReferralCommand : IRequest<bool>
{
    public Guid UserId { get; set; }
}

public class DeactivateUserReferralCommand : IRequest<bool>
{
    public Guid UserId { get; set; }
}

public class DeleteUserReferralCommand : IRequest<bool>
{
    public Guid Id { get; set; }
}