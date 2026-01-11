using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ECommerceApp.Infrastructure.Data;
using ECommerceApp.Core.Entities;
using System.Text.Json;

namespace ECommerceApp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SurveysController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public SurveysController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("submit")]
        public async Task<IActionResult> SubmitResponse([FromBody] SurveySubmissionDto dto)
        {
            try
            {
                Guid targetSurveyId = dto.SurveyId;

                if (targetSurveyId == Guid.Empty)
                {
                    var firstSurvey = await _context.Surveys.FirstOrDefaultAsync();
                    if (firstSurvey != null)
                    {
                        targetSurveyId = firstSurvey.id;
                    }
                    else
                    {
                        var newSurvey = new Survey
                        {
                            id = Guid.NewGuid(),
                            Title = "Car Rental Stats",
                            Description = "Auto-Generated",
                            CreatedAt = DateTime.UtcNow,
                            IsActive = true
                        };
                        _context.Surveys.Add(newSurvey);
                        await _context.SaveChangesAsync();
                        targetSurveyId = newSurvey.id;
                    }
                }

                var response = new SurveyResponse
                {
                    SurveyId = targetSurveyId,
                    UserId = dto.UserId,
                    SubmittedAt = DateTime.UtcNow,
                    Data = dto.Data,
                    Answers = dto.Answers?.Select(a => new SurveyAnswer
                    {
                        QuestionId = a.QuestionId,
                        AnswerValue = a.AnswerValue
                    }).ToList() ?? new List<SurveyAnswer>()
                };

                _context.SurveyResponses.Add(response);
                await _context.SaveChangesAsync();

                return Ok(new { message = "შენახულია!" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in Submit: {ex.Message}");
                if (ex.InnerException != null) Console.WriteLine($"InnerDB: {ex.InnerException.Message}");
                return StatusCode(500, $"Server Error: {ex.Message}");
            }
        }

        [HttpGet("car-stats")]
        public async Task<IActionResult> GetCarStats()
        {
            try
            {
                // === შესწორება ===
                // ამოვიღეთ "&& r.Data != string.Empty", რადგან ეს იწვევს JSON ერორს Postgres-ში.
                var responses = await _context.SurveyResponses
                                              .Where(r => r.Data != null)
                                              .ToListAsync();

                var carEntries = new List<CarEntryDto>();

                foreach (var resp in responses)
                {
                    // დამატებითი შემოწმება კოდის მხარეს (რადგან ბაზიდან უკვე წამოვიღეთ)
                    if (string.IsNullOrWhiteSpace(resp.Data)) continue;

                    try
                    {
                        var entry = JsonSerializer.Deserialize<CarEntryDto>(resp.Data, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        if (entry != null)
                        {
                            entry.Id = resp.Id;
                            carEntries.Add(entry);
                        }
                    }
                    catch (JsonException jex)
                    {
                        Console.WriteLine($"JSON Parse Error for ID {resp.Id}: {jex.Message}");
                        continue;
                    }
                }

                return Ok(carEntries);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in GetCarStats: {ex.Message}");
                return StatusCode(500, $"Database Error: {ex.Message}");
            }
        }
    }

    // DTOs
    public class SurveySubmissionDto
    {
        public Guid SurveyId { get; set; }
        public Guid? UserId { get; set; }
        public List<SurveyAnswerDto>? Answers { get; set; }
        public string? Data { get; set; }
    }

    public class SurveyAnswerDto
    {
        public Guid QuestionId { get; set; }
        public string? AnswerValue { get; set; }
    }

    public class CarEntryDto
    {
        public Guid Id { get; set; }
        public string Brand { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public int Year { get; set; }
        public decimal Price { get; set; }
        public string? Description { get; set; }
    }
}