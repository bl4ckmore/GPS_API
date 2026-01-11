using System.ComponentModel.DataAnnotations;

namespace ECommerceApp.Core.Entities
{
    public class Survey : BaseEntity
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }

        // --- CHANGED ---
        // Instead of a string "Type", we now have a Foreign Key
        public Guid SurveyTypeId { get; set; }
        public SurveyType? SurveyType { get; set; } // Navigation property
        // ----------------

        public bool IsActive { get; set; } = true;
        public ICollection<SurveyQuestion> Questions { get; set; } = new List<SurveyQuestion>();
    }
}