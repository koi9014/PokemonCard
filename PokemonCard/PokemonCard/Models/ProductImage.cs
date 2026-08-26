using System;
using System.Collections.Generic;

namespace PokemonCard.Models;

/// <summary>
/// 商品圖片
/// </summary>
public partial class ProductImage
{
    /// <summary>
    /// 圖片編號
    /// </summary>
    public int ImageId { get; set; }

    /// <summary>
    /// 商品編號
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// 圖片網址
    /// </summary>
    public string ImageUrl { get; set; } = null!;

    /// <summary>
    /// 圖片排序
    /// </summary>
    public int ImageOrder { get; set; }

    /// <summary>
    /// 圖片建立時間
    /// </summary>
    public DateTime ImgCreatedAt { get; set; }

    public virtual Product Product { get; set; } = null!;
}
