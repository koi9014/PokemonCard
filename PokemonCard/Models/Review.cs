using System;
using System.Collections.Generic;

namespace PokemonCard.Models;

/// <summary>
/// 訂單評價
/// </summary>
public partial class Review
{
    /// <summary>
    /// 評價編號
    /// </summary>
    public int ReviewId { get; set; }

    /// <summary>
    /// 訂單編號
    /// </summary>
    public int OrderId { get; set; }

    /// <summary>
    /// 評價者編號
    /// </summary>
    public int ReviewerId { get; set; }

    /// <summary>
    /// 被評價者編號
    /// </summary>
    public int RevieweeId { get; set; }

    /// <summary>
    /// 評分
    /// </summary>
    public int Rating { get; set; }

    /// <summary>
    /// 評價內容
    /// </summary>
    public string? Comment { get; set; }

    /// <summary>
    /// 評價建立時間
    /// </summary>
    public DateTime ReviewCreatedAt { get; set; }

    /// <summary>
    /// 評價更新時間
    /// </summary>
    public DateTime ReviewUpdatedAt { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual Seller Reviewee { get; set; } = null!;

    public virtual User Reviewer { get; set; } = null!;
}
