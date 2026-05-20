namespace WebApplication1.Models
{
    public class transactionTB
    {
        public int transId { get; set; }
        public int fromacno { get; set; }
        public int toacno { get; set; }
        public int amount { get; set; }
    }
    public class Transfer
    {
        public int toAcno { get; set; }
        public float amount { get; set; }
        public int transPwd { get; set; }
    }
    public class Transactions
    {
        public int acno { get; set; }
        public float amount { get; set; }
        public string type { get; set; } = String.Empty;
    }
}
