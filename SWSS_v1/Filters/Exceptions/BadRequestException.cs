using System.Web.Http.ModelBinding;

namespace SWSS_v1.Filters.Exceptions
{
    //status code 400
    public class BadRequestException : Exception
    {
        public BadRequestException(string message) : base(message)
        {

        }
    }
}
