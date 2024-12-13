namespace PosApp.Admin.Api.Data.Enums
{
    public enum PosOrderType
    {
        Maturity = 1, 
        Withdraw
    }

    public enum OrderStatus
    {
        /// <summary>
        /// Hủy
        /// </summary>
        Cancel = -1,
        /// <summary>
        /// Chưa xử lý
        /// </summary>
        Pending = 1,
        /// <summary>
        /// Đã thu phí
        /// </summary>
        FeeCharged,
        /// <summary>
        /// Đang xử lý
        /// </summary>
        Processing,
        /// <summary>
        /// Nợ phí
        /// </summary>
        FeeDebt,
        /// <summary>
        /// Đã hoàn thành
        /// </summary>
        Complete
    }
}
