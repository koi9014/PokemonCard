using System;
using System.Collections.Generic;

namespace PokemonCard.Models;

/// <summary>
/// 使用者
/// </summary>
public partial class User
{
    /// <summary>
    /// 使用者編號
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// 顯示名稱
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// 使用者名稱
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// 電子郵件
    /// </summary>
    public string Email { get; set; } = null!;

    /// <summary>
    /// 密碼雜湊值
    /// </summary>
    public string? PasswordHash { get; set; }

    /// <summary>
    /// 電話
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// 頭像
    /// </summary>
    public string? Avatar { get; set; }

    /// <summary>
    /// 生日
    /// </summary>
    public DateOnly? Birthday { get; set; }

    /// <summary>
    /// 登入提供者
    /// </summary>
    public string Provider { get; set; } = null!;

    /// <summary>
    /// 第三方登入識別碼
    /// </summary>
    public string? ProviderId { get; set; }

    /// <summary>
    /// 使用者狀態
    /// </summary>
    public string UserStatus { get; set; } = null!;

    /// <summary>
    /// 賣家驗證狀態
    /// </summary>
    public string SellerVerificationStatus { get; set; } = null!;

    /// <summary>
    /// 最後登入時間
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// 建立時間
    /// </summary>
    public DateTime UserCreatedAt { get; set; }

    /// <summary>
    /// 更新時間
    /// </summary>
    public DateTime UserUpdatedAt { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    public virtual Seller? Seller { get; set; }

    public virtual SellerApplication? SellerApplication { get; set; }

    public virtual ICollection<UserBlacklist> UserBlacklists { get; set; } = new List<UserBlacklist>();
}
