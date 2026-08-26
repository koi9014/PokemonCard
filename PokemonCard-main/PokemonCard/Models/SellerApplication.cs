using System;
using System.Collections.Generic;

namespace PokemonCard.Models;

/// <summary>
/// 賣家申請
/// </summary>
public partial class SellerApplication
{
    /// <summary>
    /// 申請編號
    /// </summary>
    public int ApplicationId { get; set; }

    /// <summary>
    /// 使用者編號
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// 真實姓名
    /// </summary>
    public string RealName { get; set; } = null!;

    /// <summary>
    /// 身分證字號
    /// </summary>
    public string IdNumber { get; set; } = null!;

    /// <summary>
    /// 聯絡電話
    /// </summary>
    public string ContactPhone { get; set; } = null!;

    /// <summary>
    /// 銀行代碼
    /// </summary>
    public string BankCode { get; set; } = null!;

    /// <summary>
    /// 銀行帳號
    /// </summary>
    public string BankAccount { get; set; } = null!;

    /// <summary>
    /// 賣家申請狀態
    /// </summary>
    public string SellerStatus { get; set; } = null!;

    /// <summary>
    /// 申請時間
    /// </summary>
    public DateTime ApplyAt { get; set; }

    /// <summary>
    /// 身分證正面照片
    /// </summary>
    public string? IdcardFront { get; set; }

    /// <summary>
    /// 身分證反面照片
    /// </summary>
    public string? IdcardBack { get; set; }

    public virtual ICollection<SellerApplicationAudit> SellerApplicationAudits { get; set; } = new List<SellerApplicationAudit>();

    public virtual User User { get; set; } = null!;
}
