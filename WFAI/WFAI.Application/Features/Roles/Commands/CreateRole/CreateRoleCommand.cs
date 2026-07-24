namespace WFAI.Application.Features.Roles.Commands
{
    public class CreateRoleRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class CreateRoleCommand : IRequest<IResponseWrapper>, IValidateMe 
    {
        public required CreateRoleRequest CreateRole { get; set; }
    }

    public class CreateRoleCommandHandler : IRequestHandler<CreateRoleCommand, IResponseWrapper>
    {
        private readonly IRoleService _roleService;

        public CreateRoleCommandHandler(IRoleService roleService)
        {
            _roleService = roleService;
        }

        public async ValueTask<IResponseWrapper> Handle(CreateRoleCommand request, CancellationToken ct)
        {
            return await _roleService.CreateRoleAsync(request.CreateRole);
        }
    }
}