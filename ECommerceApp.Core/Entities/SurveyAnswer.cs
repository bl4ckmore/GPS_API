namespace ECommerceApp.Core.Entities
{
    public class SurveyAnswer
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ResponseId { get; set; }
        public Guid QuestionId { get; set; }

        // Changed to string? because the answer might be optional or skipped
        public string? AnswerValue { get; set; }
    }
}