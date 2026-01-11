using System.ComponentModel.DataAnnotations;

namespace ECommerceApp.Core.Entities
{
    public class SurveyType
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty; // e.g., "Car Rental Stats"

        [Required]
        [MaxLength(50)]
        public string Code { get; set; } = string.Empty; // e.g., "RENTAL_STATS"

        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}