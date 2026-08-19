using System;
using System.Collections.Generic;

namespace PokemonCard.Models;

/// <summary>
/// 訂單狀態歷程
/// </summary>
public partial class OrderHistory
{
    /// <summary>
    /// 歷程編號
    /// </summary>
    public int HistoryId { get; set; }

    /// <summary>
    /// 訂單號碼
    /// </summary>
    public string OrderNo { get; set; } = null!;

    /// <summary>
    /// 訂單狀態
    /// </summary>
    public string OrderStatus { get; set; } = null!;

    /// <summary>
    /// 狀態變更時間
    /// </summary>
    public DateTime ChangeTime { get; set; }

    public virtual Order OrderNoNavigation { get; set; } = null!;
}
