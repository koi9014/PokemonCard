using System;
using System.Collections.Generic;

namespace PokemonCard.Models;

/// <summary>
/// 使用者黑名單
/// </summary>
public partial class UserBlacklist
{
    /// <summary>
    /// 封鎖紀錄編號
    /// </summary>
    public int BlockId { get; set; }

    /// <summary>
    /// 使用者編號
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// 封鎖原因
    /// </summary>
    public string? ReasonDetail { get; set; }

    /// <summary>
    /// 封鎖狀態
    /// </summary>
    public string BlockStatus { get; set; } = null!;

    /// <summary>
    /// 管理員編號
    /// </summary>
    public int AdminId { get; set; }

    /// <summary>
    /// 封鎖時間
    /// </summary>
    public DateTime BlockedAt { get; set; }

    /// <summary>
    /// 解除封鎖時間
    /// </summary>
    public DateTime? UnblockedAt { get; set; }

    public virtual AdminUser Admin { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
