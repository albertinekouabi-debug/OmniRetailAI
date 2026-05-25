using OmniRetail.Core.Enums;

namespace OmniRetail.Core.Entities;

/// <summary>Permission granulaire RBAC — Format : module.action</summary>
public class Permission : BaseEntity
{
    public string Name        { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Module      { get; set; } = string.Empty;

    public ICollection<RolePermission> RolePermissions { get; set; }
        = new List<RolePermission>();
}

/// <summary>Association Rôle ↔ Permission</summary>
public class RolePermission : BaseEntity
{
    public Role   Role         { get; set; }
    public Guid   PermissionId { get; set; }
    public Permission Permission { get; set; } = null!;
}
