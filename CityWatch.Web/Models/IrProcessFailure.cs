namespace CityWatch.Web.Models
{
    public class IrProcessFailure
    {
        public IrProcessFailure()
        { 
        }

        public IrProcessFailure(string errorMessage, string stackTrace)
        {
            ErrorMessage = errorMessage;
            StackTrace = stackTrace;
        }

        public string ErrorMessage { get; set; }

        public string StackTrace { get; set; }
    }
}
