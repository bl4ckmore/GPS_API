using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECommerceApp.Core.Entities
{
    public class SurveyQuestion : BaseEntity
    {
        public Guid SurveyId { get; set; }

        [Required]
        public string QuestionText { get; set; } = string.Empty; // Initialized

        [Required]
        public string QuestionType { get; set; } = "Text"; // Default value

        public int Order { get; set; }
        public bool IsRequired { get; set; }

        [Column(TypeName = "jsonb")]
        public string? Options { get; set; }
    }
}