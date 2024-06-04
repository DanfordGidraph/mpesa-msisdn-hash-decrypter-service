using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MPESA_V2_APIV2_MSISDN_DECRYPTER
{
    [Keyless]
    public record PhoneNumber(string Hash, string Msisdn);
}