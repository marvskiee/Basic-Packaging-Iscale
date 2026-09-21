namespace ISCALE.MVVM.Model
{
    public class TransactionSearchPayload
    {
        public string Weigher { get; set; }
        public bool IsWeigherSpecific { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
