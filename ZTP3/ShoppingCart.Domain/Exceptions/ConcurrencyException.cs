namespace ShoppingCart.Domain.Exceptions
{
    public class ConcurrencyException : Exception
    {
        public ConcurrencyException() 
            : base("The item has been modified by another process. Please retry the operation.")
        {
        }

        public ConcurrencyException(string message) : base(message)
        {
        }

        public ConcurrencyException(string message, Exception innerException) 
            : base(message, innerException)
        {
        }
    }
}
