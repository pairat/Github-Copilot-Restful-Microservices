using System.ComponentModel.DataAnnotations;

namespace CustomerService.Models
{
    public class CustomerCreateDto
    {
        [Required]
        [StringLength(50)]
        public string FirstName { get; set; } = null!;

        [Required]
        [StringLength(50)]
        public string LastName { get; set; } = null!;

        [Required]
        [EmailAddress]
        public string EmailAddress { get; set; } = null!;
    }
}
