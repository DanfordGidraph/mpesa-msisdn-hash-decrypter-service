using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MPESA_V2_APIV2_MSISDN_DECRYPTER
{
    public class User(string name, string email, string role)
    {

        [Required]
        [Column(TypeName = "varchar(30)")]
        public string Name { get; set; } = name;

        [Required]
        [Column(TypeName = "varchar(30)")]
        public string Email { get; set; } = email;

        [Required]
        [Column(TypeName = "varchar(30)")]
        public string Role { get; set; } = role;
    }

    public record UserRecord(string Name, string Email, string Role, string Token)
    {
        [Key]
        [Column(TypeName = "varchar(30)")]
        public string Name { get; set; } = Name;

        [Required]
        [Column(TypeName = "varchar(30)")]
        public string Email { get; set; } = Email;

        [Required]
        [Column(TypeName = "varchar(30)")]
        public string Role { get; set; } = Role;

        [Required]
        [Column(TypeName = "varchar(30)")]
        public string Token { get; set; } = Token;

    }

}