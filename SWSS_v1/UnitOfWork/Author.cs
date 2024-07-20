namespace SWSS_v1.UnitOfBox
{
    public class Author
    {
        public int Id { get; set; }
        public int DepartmentId { get; set; }
        public string Name { get; set; }
        //One Employee return only one Department
        //One Department return all employees related to Department
        public Department? Employee { get; set; }
    }
}
