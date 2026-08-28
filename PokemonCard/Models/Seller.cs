using System;
using System.Collections.Generic;
using System.Linq;

namespace PokemonCard.Models;

/// <summary>
/// 賣家
/// </summary>
public partial class Seller
{
    /// <summary>
    /// 使用者編號
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// 姓名
    /// </summary>
    public string FullName { get; set; } = null!;

    /// <summary>
    /// 商店名稱
    /// </summary>
    public string StoreName { get; set; } = null!;

    /// <summary>
    /// 商店介紹
    /// </summary>
    public string? StoreDescription { get; set; }

    /// <summary>
    /// 商店狀態
    /// </summary>
    public string StoreStatus { get; set; } = null!;

    /// <summary>
    /// 商店建立時間
    /// </summary>
    public DateTime StoreCreatedAt { get; set; }

    /// <summary>
    /// 商店更新時間
    /// </summary>
    public DateTime StoreUpdatedAt { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    public virtual User User { get; set; } = null!;
}
