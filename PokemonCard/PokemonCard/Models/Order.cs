using System;
using System.Collections.Generic;

namespace PokemonCard.Models;

/// <summary>
/// 訂單
/// </summary>
public partial class Order
{
    /// <summary>
    /// 訂單編號
    /// </summary>
    public int OrderId { get; set; }

    /// <summary>
    /// 訂單號碼
    /// </summary>
    public string OrderNo { get; set; } = null!;

    /// <summary>
    /// 買家編號
    /// </summary>
    public int BuyerId { get; set; }

    /// <summary>
    /// 賣家編號
    /// </summary>
    public int SellerId { get; set; }

    /// <summary>
    /// 下單時間
    /// </summary>
    public DateTime OrderedAt { get; set; }

    /// <summary>
    /// 訂金
    /// </summary>
    public int OrderDeposit { get; set; }

    /// <summary>
    /// 訂單金額
    /// </summary>
    public int OrderAmount { get; set; }

    /// <summary>
    /// 運費
    /// </summary>
    public int ShipAmount { get; set; }

    /// <summary>
    /// 訂單狀態
    /// </summary>
    public string OrderStatus { get; set; } = null!;

    /// <summary>
    /// 收件人姓名
    /// </summary>
    public string ReceiverName { get; set; } = null!;

    /// <summary>
    /// 收件人電話
    /// </summary>
    public string ReceiverPhone { get; set; } = null!;

    /// <summary>
    /// 配送地址
    /// </summary>
    public string ShippingAddress { get; set; } = null!;

    /// <summary>
    /// 訂單建立時間
    /// </summary>
    public DateTime OrderCreatedAt { get; set; }

    /// <summary>
    /// 訂單更新時間
    /// </summary>
    public DateTime? OrderUpdatedAt { get; set; }

    public virtual User Buyer { get; set; } = null!;

    public virtual Logistic? Logistic { get; set; }

    public virtual MoneyReconciliation? MoneyReconciliation { get; set; }

    public virtual ICollection<OrderHistory> OrderHistories { get; set; } = new List<OrderHistory>();

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    public virtual Seller Seller { get; set; } = null!;
}
