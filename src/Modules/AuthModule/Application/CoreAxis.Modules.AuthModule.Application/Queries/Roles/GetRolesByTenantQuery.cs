using CoreAxis.Modules.AuthModule.Application.DTOs;
using CoreAxis.Modules.AuthModule.Domain.Repositories;
using CoreAxis.SharedKernel;
using MediatR;

namespace CoreAxis.Modules.AuthModule.Application.Queries.Roles;

public record GetRolesByTenantQuery(Guid TenantId) : IRequest<Result<IEnumerable<RoleDto>>>;

public class GetRolesByTenantQueryHandler : IRequestHandler<GetRolesByTenantQuery, Result<IEnumerable<RoleDto>>>
{
    private readonly IRoleRepository _roleRepository;

    public GetRolesByTenantQueryHandler(IRoleRepository roleRepository)
    {
        _roleRepository = roleRepository;
    }

    public async Task<Result<IEnumerable<RoleDto>>> Handle(GetRolesByTenantQuery request, CancellationToken cancellationToken)
    {
        var roles = await _roleRepository.GetByTenantAsync(request.TenantId, cancellationToken);
        
        var roleDtos = roles.Select(role => new RoleDto
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description,
            IsActive = role.IsActive,
            IsSystemRole = role.IsSystemRole,
            CreatedAt = role.CreatedOn,
            TenantId = role.TenantId ?? Guid.Empty
        }).ToList();

        return Result<IEnumerable<RoleDto>>.Success(roleDtos);
    }
}