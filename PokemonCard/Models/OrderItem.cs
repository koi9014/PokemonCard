using System;
using System.Collections.Generic;

namespace PokemonCard.Models;

/// <summary>
/// 訂單明細
/// </summary>
public partial class OrderItem
{
    /// <summary>
    /// 訂單明細編號
    /// </summary>
    public int OrderItemsId { get; set; }

    /// <summary>
    /// 訂單編號
    /// </summary>
    public int OrderId { get; set; }

    /// <summary>
    /// 商品編號
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// 商品名稱
    /// </summary>
    public string ProductName { get; set; } = null!;

    /// <summary>
    /// 商品規格一
    /// </summary>
    public string? ProductSpec { get; set; }

    /// <summary>
    /// 商品規格二
    /// </summary>
    public string? ProductSpec2 { get; set; }

    /// <summary>
    /// 是否預售
    /// </summary>
    public bool PreSale { get; set; }

    /// <summary>
    /// 購買數量
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// 單價
    /// </summary>
    public int UnitPrice { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;
}
