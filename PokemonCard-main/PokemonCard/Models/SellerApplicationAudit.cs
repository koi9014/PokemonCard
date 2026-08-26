using System;
using System.Collections.Generic;

namespace PokemonCard.Models;

/// <summary>
/// 賣家申請審核紀錄
/// </summary>
public partial class SellerApplicationAudit
{
    /// <summary>
    /// 審核紀錄編號
    /// </summary>
    public int AuditId { get; set; }

    /// <summary>
    /// 申請編號
    /// </summary>
    public int ApplicationId { get; set; }

    /// <summary>
    /// 審核狀態
    /// </summary>
    public string AuditStatus { get; set; } = null!;

    /// <summary>
    /// 管理員編號
    /// </summary>
    public int AdminId { get; set; }

    /// <summary>
    /// 審核時間
    /// </summary>
    public DateTime ReviewedAt { get; set; }

    /// <summary>
    /// 審核備註
    /// </summary>
    public string? AuditNote { get; set; }

    public virtual AdminUser Admin { get; set; } = null!;

    public virtual SellerApplication Application { get; set; } = null!;
}
