using System;
using System.Collections.Generic;

namespace PokemonCard.Models;

/// <summary>
/// 禁用詞
/// </summary>
public partial class BannedWord
{
    /// <summary>
    /// 禁用詞編號
    /// </summary>
    public int BannedWordsId { get; set; }

    /// <summary>
    /// 禁用詞
    /// </summary>
    public string BannedWords { get; set; } = null!;

    /// <summary>
    /// 是否啟用
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// 管理員編號
    /// </summary>
    public int AdminId { get; set; }

    /// <summary>
    /// 禁用詞建立時間
    /// </summary>
    public DateTime BanCreatedAt { get; set; }

    public virtual AdminUser Admin { get; set; } = null!;
}
