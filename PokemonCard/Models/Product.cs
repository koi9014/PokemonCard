using System;
using System.Collections.Generic;

namespace PokemonCard.Models;

/// <summary>
/// 商品
/// </summary>
public partial class Product
{
    /// <summary>
    /// 商品編號
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// 賣家編號
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// 商品名稱
    /// </summary>
    public string ProductName { get; set; } = null!;

    /// <summary>
    /// 商品所在地
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// 商品類型
    /// </summary>
    public string? ProductType { get; set; }

    /// <summary>
    /// 商品描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 發布時間
    /// </summary>
    public DateTime? PublishedAt { get; set; }

    /// <summary>
    /// 商品狀態
    /// </summary>
    public string? ProductStatus { get; set; }

    /// <summary>
    /// 建立時間
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 更新時間
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();

    public virtual ICollection<ProductSpec> ProductSpecs { get; set; } = new List<ProductSpec>();

    public virtual Seller Seller { get; set; } = null!;
}
