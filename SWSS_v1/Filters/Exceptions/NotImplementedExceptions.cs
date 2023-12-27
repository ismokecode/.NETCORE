using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace SWSS_v1.Filters.Exceptions
{
    //status code 501
    public class NotImplementedExceptions : Exception
    {
        public NotImplementedExceptions(string message) : base(message)
        {


        }
    }
}
