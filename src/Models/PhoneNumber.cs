using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MPESA_V2_APIV2_MSISDN_DECRYPTER
{
    public record PhoneNumber(string Hash, string Msisdn)
    {
        [Key]
        [Required]
        [Column("hash", TypeName = "TEXT")]
        public string Hash { get; set; } = Hash;

        [Required]
        [Column("msisdn", TypeName = "TEXT")]
        public string Msisdn { get; set; } = Msisdn;
    }
}