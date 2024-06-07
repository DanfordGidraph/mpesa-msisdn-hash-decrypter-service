using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MPESA_V2_APIV2_MSISDN_DECRYPTER
{
    public class User(string name, string email, string role)
    {

        public string Name { get; set; } = name;

        public string Email { get; set; } = email;

        public string Role { get; set; } = role;
    }

    public record UserRecord(string Email, string Name, string Role, string Token)
    {
        [Key]
        [Required]
        [Column("email", TypeName = "TEXT")]
        public string Email { get; set; } = Email;

        [Column("name", TypeName = "TEXT")]
        public string Name { get; set; } = Name;

        [Required]
        [Column("role", TypeName = "TEXT")]
        public string Role { get; set; } = Role;

        [Required]
        [Column("token", TypeName = "TEXT")]
        public string Token { get; set; } = Token;

    }

}