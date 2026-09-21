namespace ISCALE.MVVM.Model
{
    public class ServiceResult
    {
        public bool Success { get; set; }
        public object Data { get; set; }
        public string ErrorMessage { get; set; }
        public string ErrorType { get; set; }
    }
}
