namespace PosApp.Admin.Api.Data.Enums
{
    public enum TransactionType
    {
        /// <summary>
        /// Nộp phí
        /// </summary>
        PayFee = 1,
        /// <summary>
        /// Nộp tiền
        /// </summary>
        Payment,
        /// <summary>
        /// Quẹt thẻ
        /// </summary>
        SwipeCard,
        /// <summary>
        /// Chuyển tiền
        /// </summary>
        Transfer,
        /// <summary>
        /// Đối ứng
        /// </summary>
        Reciprocal,
        /// <summary>
        /// Chuyển tiền Cộng tác viên
        /// </summary>
        TransferCollaborator,
        /// <summary>
        /// Chuyển tiền Đối tác
        /// </summary>
        TransferPartner
    }

    public enum TransactionStatus
    {
        /// <summary>
        /// Hủy
        /// </summary>
        Cancel = -1,
        /// <summary>
        /// Đang xử lý
        /// </summary>
        Processing,
        /// <summary>
        /// Hoàn thành
        /// </summary>
        Complete,
    }
}
