using SWSS_v1.UnitOfBox;

namespace SWSS_v1.UnitOfWork
{
    public interface ILocationRepository : IRepository<Location>
    {
        bool IsExist(Location loc);
    }
}
