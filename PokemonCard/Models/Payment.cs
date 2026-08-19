using System;
using System.Collections.Generic;

namespace PokemonCard.Models;

/// <summary>
/// 付款紀錄
/// </summary>
public partial class Payment
{
    /// <summary>
    /// 付款編號
    /// </summary>
    public int PaymentId { get; set; }

    /// <summary>
    /// 訂單編號
    /// </summary>
    public int OrderId { get; set; }

    /// <summary>
    /// 付款類型
    /// </summary>
    public string PaymentType { get; set; } = null!;

    /// <summary>
    /// 付款金額
    /// </summary>
    public int Amount { get; set; }

    /// <summary>
    /// 付款方式
    /// </summary>
    public string PaymentMethod { get; set; } = null!;

    /// <summary>
    /// 交易編號
    /// </summary>
    public string? TransactionNo { get; set; }

    /// <summary>
    /// 付款狀態
    /// </summary>
    public string PaymentStatus { get; set; } = null!;

    /// <summary>
    /// 付款時間
    /// </summary>
    public DateTime? PaidAt { get; set; }

    /// <summary>
    /// 付款紀錄建立時間
    /// </summary>
    public DateTime PayCreatedAt { get; set; }

    /// <summary>
    /// 付款紀錄更新時間
    /// </summary>
    public DateTime? PayUpdatedAt { get; set; }

    public virtual Order Order { get; set; } = null!;
}
