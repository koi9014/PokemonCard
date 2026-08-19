using System;
using System.Collections.Generic;

namespace PokemonCard.Models;

/// <summary>
/// 管理員
/// </summary>
public partial class AdminUser
{
    /// <summary>
    /// 管理員編號
    /// </summary>
    public int AdminId { get; set; }

    /// <summary>
    /// 管理員帳號
    /// </summary>
    public string Username { get; set; } = null!;

    /// <summary>
    /// 姓名
    /// </summary>
    public string FullName { get; set; } = null!;

    /// <summary>
    /// 角色編號
    /// </summary>
    public int RoleId { get; set; }

    /// <summary>
    /// 電子郵件
    /// </summary>
    public string Email { get; set; } = null!;

    /// <summary>
    /// 電話
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// 密碼雜湊值
    /// </summary>
    public string PasswordHash { get; set; } = null!;

    /// <summary>
    /// 是否鎖定
    /// </summary>
    public bool IsLocked { get; set; }

    /// <summary>
    /// 最後登入時間
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    public virtual ICollection<BannedWord> BannedWords { get; set; } = new List<BannedWord>();

    public virtual ICollection<MoneyReconciliation> MoneyReconciliations { get; set; } = new List<MoneyReconciliation>();

    public virtual Role Role { get; set; } = null!;

    public virtual ICollection<SellerApplicationAudit> SellerApplicationAudits { get; set; } = new List<SellerApplicationAudit>();

    public virtual ICollection<UserBlacklist> UserBlacklists { get; set; } = new List<UserBlacklist>();
}
