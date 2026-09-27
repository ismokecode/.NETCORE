using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SWSS_v1.Models
{
    public class Institute
    {
        public int InstituteId { get; set; }
        public string Name { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public bool isActive { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string? ModifiedBy { get; set; }
        public string? CreatedBy { get; set; }
        // Maps seamlessly to the 'imageFile' key appended in Angular
        [NotMapped]
        public IFormFile? ImageFile { get; set; }
        public string? ImageFilePath { get; set; }
    }
}
