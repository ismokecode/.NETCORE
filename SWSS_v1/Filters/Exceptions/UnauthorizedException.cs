namespace SWSS_v1.Filters.Exceptions
{
    //status code 401
    public class UnauthorizedException : Exception
    {
        public UnauthorizedException(string message) : base(message)
        {

        }
    }
}
