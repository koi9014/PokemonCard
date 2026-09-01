using System;
using System.Collections.Generic;

namespace PokemonCard.Models;

/// <summary>
/// 金流對帳
/// </summary>
public partial class MoneyReconciliation
{
    /// <summary>
    /// 對帳編號
    /// </summary>
    public int MoneyId { get; set; }

    /// <summary>
    /// 訂單編號
    /// </summary>
    public int OrderId { get; set; }

    /// <summary>
    /// 訂單金額
    /// </summary>
    public int OrderAmount { get; set; }

    /// <summary>
    /// 賣家撥款金額
    /// </summary>
    public int SellerPayout { get; set; }

    /// <summary>
    /// 平台收入
    /// </summary>
    public int PlatformRevenue { get; set; }

    /// <summary>
    /// 人工調整金額
    /// </summary>
    public int AdjustAmount { get; set; }

    /// <summary>
    /// 是否人工調整
    /// </summary>
    public bool IsManual { get; set; }

    /// <summary>
    /// 調整原因
    /// </summary>
    public string? AdjustReason { get; set; }

    /// <summary>
    /// 管理員編號
    /// </summary>
    public int? AdminId { get; set; }

    /// <summary>
    /// 對帳建立時間
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 撥款狀態
    /// </summary>
    public string RemitStatus { get; set; } = null!;

    /// <summary>
    /// 撥款結果
    /// </summary>
    public string? RemitResult { get; set; }

    /// <summary>
    /// 撥款日期
    /// </summary>
    public DateTime? RemitDate { get; set; }

    public virtual AdminUser? Admin { get; set; }

    public virtual Order Order { get; set; } = null!;
}

public class RevenueViewModel
{
    public string OrderNo { get; set; } = string.Empty;   // 訂單編號
    public string? RemitResult { get; set; }               // 狀態/撥款結果
    public string? RemitStatus { get; set; }
    public decimal OrderAmount { get; set; }              // 訂單金額
    public decimal PlatformRevenue { get; set; }          // 平台服務費
    public decimal AdjustAmount { get; set; }            // 調整金額
    public decimal SellerPayout { get; set; }            // 實際撥款金額
    public DateTime? RemitDate { get; set; }              // 入帳日期
}
