using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace CityWatch.Data.Models
{
    public class User
    {
        public static ClaimsIdentity Identity { get; internal set; }
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "User Name")]
        public string UserName { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        public bool IsAdmin { get; set; }

        public bool IsDeleted { get; set; }
    }
}
