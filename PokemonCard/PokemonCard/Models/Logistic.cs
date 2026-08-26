using System;
using System.Collections.Generic;

namespace PokemonCard.Models;

/// <summary>
/// 物流
/// </summary>
public partial class Logistic
{
    /// <summary>
    /// 訂單號碼
    /// </summary>
    public string OrderNo { get; set; } = null!;

    /// <summary>
    /// 配送方式
    /// </summary>
    public string ShipWay { get; set; } = null!;

    /// <summary>
    /// 物流單號
    /// </summary>
    public string? ShipNumber { get; set; }

    /// <summary>
    /// 物流狀態
    /// </summary>
    public string ShipStatus { get; set; } = null!;

    /// <summary>
    /// 物流更新時間
    /// </summary>
    public DateTime ShipUpdatedAt { get; set; }

    public virtual Order OrderNoNavigation { get; set; } = null!;
}
