using SWSS_v1.UnitOfBox;

namespace SWSS_v1.UnitOfWork
{
    public interface ITestLinkRepository: IRepository<TestLink>
    {
        void SaveTestLinkAsync(TestLink obj, out IEnumerable<TestLink> returnVal);
        Task SaveTestLinkAsync(List<TestLink> linkDetails);
        Task<TestLink> GetByIdAsync(string urlId);
        bool isEarlier(DateTime expiryTime);
    }
}
