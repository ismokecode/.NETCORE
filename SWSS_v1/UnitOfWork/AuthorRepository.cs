namespace SWSS_v1.UnitOfBox
{
    public class AuthorRepository : IRepository<Author>
    {
        UnitOfWork entities = new UnitOfWork(new CustomDbContext());
        //Write code here to implement the members of the IRepository interface
        public void Add(Author entity)
        {
            entities.Repository<Author>().Add(entity);
        }

        public IList<Author> GetAll()
        {
            return entities.Repository<Author>().GetAll();
        }

        public Author GetById(object id)
        {
            return entities.Repository<Author>().GetById(id);
        }
    }
}
