using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
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

        [NotMapped]
        public DateTime? LastLoginDate { get; set; } = null;

        [NotMapped]
        public string FormattedLastLoginDate
        {
            get
            {
                return LastLoginDate.HasValue
                    ? LastLoginDate.Value.ToString("dd/MM/yyyy HH:mm:ss")
                    : null;
            }
        }

        [NotMapped]
        public string LastLoginIPAdress { get; set; }
    }
}
