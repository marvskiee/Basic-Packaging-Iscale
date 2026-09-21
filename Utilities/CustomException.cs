namespace ISCALE.Utilities
{
    public class CustomException : Exception
    {
        public string ErrorType { get; }
        public CustomException(string message, string errorType) : base(message)
        {
            ErrorType = errorType;
        }
    }
}
