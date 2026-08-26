using System;
using System.Collections.Generic;

namespace PokemonCard.Models;

/// <summary>
/// 管理員角色
/// </summary>
public partial class Role
{
    /// <summary>
    /// 角色編號
    /// </summary>
    public int RoleId { get; set; }

    /// <summary>
    /// 角色名稱
    /// </summary>
    public string RoleName { get; set; } = null!;

    /// <summary>
    /// 角色說明
    /// </summary>
    public string? RoleDescription { get; set; }

    public virtual ICollection<AdminUser> AdminUsers { get; set; } = new List<AdminUser>();
}
