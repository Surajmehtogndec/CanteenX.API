namespace CanteenX.API.Model;

public partial class Canteenpaymentaccountsdetails
{
    public int Id { get; set; }
    public int CanteenId { get; set; }
    public string? AccountHolderName { get; set; }
    public string? AccountNumber { get; set; }
    public string? IFSC { get; set; }
    public string? BankName { get; set; }
    public string? RazorpayAccountId { get; set; }
    public string? RazorpayFundAccountId { get; set; }
    public string? Status { get; set; } = "Pending";
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
   
   public virtual Canteenpaymentaccountsdetails? Canteen { get; set; }




}
