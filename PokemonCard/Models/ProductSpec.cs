using System;
using System.Collections.Generic;

namespace PokemonCard.Models;

/// <summary>
/// 商品規格
/// </summary>
public partial class ProductSpec
{
    /// <summary>
    /// 規格編號
    /// </summary>
    public int SpecificationId { get; set; }

    /// <summary>
    /// 商品編號
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// 規格類別一
    /// </summary>
    public string SpecsCategory1 { get; set; } = null!;

    /// <summary>
    /// 規格類別二
    /// </summary>
    public string? SpecsCategory2 { get; set; }

    /// <summary>
    /// 規格價格
    /// </summary>
    public int SpecsPrice { get; set; }

    public int? Deposit { get; set; }

    /// <summary>
    /// 庫存數量
    /// </summary>
    public int Stock { get; set; }

    /// <summary>
    /// 是否預售
    /// </summary>
    public bool PreSale { get; set; }

    /// <summary>
    /// 規格建立時間
    /// </summary>
    public DateTime SpecsCreatedAt { get; set; }

    /// <summary>
    /// 規格更新時間
    /// </summary>
    public DateTime SpecsUpdatedAt { get; set; }

    public virtual Product Product { get; set; } = null!;
}
