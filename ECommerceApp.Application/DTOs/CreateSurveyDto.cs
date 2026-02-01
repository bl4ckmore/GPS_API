// Add this to the bottom of SurveysController.cs
    public class CreateSurveyDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid SurveyTypeId { get; set; }
        public bool IsActive { get; set; } = true;
    }