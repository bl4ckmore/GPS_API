using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECommerceApp.Core.Entities
{
    public class SurveyResponse
    {
        [Key]
        [Column("id")] // Maps to 'id' in Postgres
        public Guid Id { get; set; } = Guid.NewGuid();

        [Column("survey_id")] // Maps to 'survey_id'
        public Guid SurveyId { get; set; }

        [Column("user_id")]
        public Guid? UserId { get; set; }

        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        // New Column: Stores structured data for analytics
        [Column("data", TypeName = "jsonb")]
        public string? Data { get; set; }

        public ICollection<SurveyAnswer> Answers { get; set; } = new List<SurveyAnswer>();
    }
}