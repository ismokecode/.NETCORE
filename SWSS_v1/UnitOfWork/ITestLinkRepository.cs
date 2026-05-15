using SWSS_v1.UnitOfBox;

namespace SWSS_v1.UnitOfWork
{
    public interface ITestLinkRepository: IRepository<TestLink>
    {
        Task SaveTestLinkAsync(TestLink obj);
    }
}
