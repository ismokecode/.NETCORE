using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SWSS_v1.Models
{
    public class RefreshToken
    {
        /// <summary>
        /// Identify for the refresh token
        /// </summary>
        [Key]
        public int Id { get; set; }
        /// <summary>
        /// Refresh token
        /// </summary>
        public string token { get; set; }
        public string JwtId { get; set; }
        /// <summary>
        /// If refresh token because if token gets stolen then revoke this token from db
        /// </summary>
        public bool IsRevoked { get; set; }
        public DateTime DateAdded { get; set; }
        public DateTime DateExpire { get; set; }
        /// <summary>
        /// below properties establish a relationship between refresh token and user in
        /// </summary>
        public string UserId { get; set; }
        [ForeignKey(nameof(UserId))]
        public ApplicationUser User { get; set; }
    }
}
