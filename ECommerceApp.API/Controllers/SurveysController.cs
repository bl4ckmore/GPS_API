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

        // ============================================================
        // 1. PUBLIC ENDPOINTS (Statistics & Submit)
        // ============================================================

        [HttpGet("definition/{typeCode}")]
        public async Task<IActionResult> GetDefinition(string typeCode)
        {
            var survey = await _context.Surveys
                .Include(s => s.SurveyType)
                .Include(s => s.Questions)
                .Where(s => s.IsActive && s.SurveyType.Code == typeCode)
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefaultAsync();

            if (survey == null) return Ok(null);

            return Ok(new
            {
                SurveyId = survey.id,
                Title = survey.Title,
                Questions = survey.Questions.OrderBy(q => q.Order).Select(q => new
                {
                    Id = q.id,
                    Text = q.QuestionText,
                    Type = q.QuestionType,
                    IsRequired = q.IsRequired,
                    Options = string.IsNullOrEmpty(q.Options) ? new List<string>() : JsonSerializer.Deserialize<List<string>>(q.Options, (JsonSerializerOptions)null)
                })
            });
        }

        [HttpPost("submit")]
        public async Task<IActionResult> SubmitResponse([FromBody] SurveySubmissionDto dto)
        {
            try
            {
                var response = new SurveyResponse
                {
                    SurveyId = dto.SurveyId,
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
                return Ok(new { message = "Saved" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("car-stats")]
        public async Task<IActionResult> GetCarStats()
        {
            try
            {
                var responses = await _context.SurveyResponses
                    .Where(r => r.Data != null)
                    .OrderByDescending(r => r.SubmittedAt)
                    .ToListAsync();

                var entries = new List<CarEntryDto>();
                var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                foreach (var r in responses)
                {
                    if (string.IsNullOrWhiteSpace(r.Data)) continue;
                    try
                    {
                        var entry = JsonSerializer.Deserialize<CarEntryDto>(r.Data, opts);
                        if (entry != null) { entry.Id = r.Id; entries.Add(entry); }
                    }
                    catch { }
                }
                return Ok(entries);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        // ============================================================
        // 2. ADMIN ENDPOINTS (This fixes the 405 Error)
        // ============================================================

        // GET: api/surveys
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var list = await _context.Surveys
                .Include(s => s.SurveyType)
                .Include(s => s.Questions)
                .OrderByDescending(s => s.CreatedAt)
                .Select(s => new
                {
                    s.id,
                    s.Title,
                    s.Description,
                    s.SurveyTypeId,
                    s.IsActive,
                    s.CreatedAt,
                    SurveyType = s.SurveyType != null ? new { Name = s.SurveyType.Name } : null,
                    Questions = s.Questions
                })
                .ToListAsync();

            return Ok(list);
        }

        // POST: api/surveys  <-- THIS WAS MISSING OR NOT RUNNING
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateSurveyDto dto)
        {
            var survey = new Survey
            {
                Title = dto.Title,
                Description = dto.Description,
                SurveyTypeId = dto.SurveyTypeId,
                IsActive = dto.IsActive,
                CreatedAt = DateTime.UtcNow
            };
            _context.Surveys.Add(survey);
            await _context.SaveChangesAsync();
            return Ok(survey);
        }

        // PUT: api/surveys/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] CreateSurveyDto dto)
        {
            var survey = await _context.Surveys.FindAsync(id);
            if (survey == null) return NotFound();

            survey.Title = dto.Title;
            survey.Description = dto.Description;
            survey.SurveyTypeId = dto.SurveyTypeId;
            survey.IsActive = dto.IsActive;

            await _context.SaveChangesAsync();
            return Ok(survey);
        }

        // DELETE: api/surveys/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var survey = await _context.Surveys.FindAsync(id);
            if (survey == null) return NotFound();

            var hasResponses = await _context.SurveyResponses.AnyAsync(r => r.SurveyId == id);
            if (hasResponses)
            {
                var responses = _context.SurveyResponses.Where(r => r.SurveyId == id);
                _context.SurveyResponses.RemoveRange(responses);
            }

            _context.Surveys.Remove(survey);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Deleted" });
        }

        // ============================================================
        // 3. BUILDER ENDPOINTS (For Question Editing)
        // ============================================================

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var s = await _context.Surveys
                .Include(s => s.Questions)
                .FirstOrDefaultAsync(x => x.id == id);

            if (s == null) return NotFound();

            return Ok(new
            {
                s.id,
                s.Title,
                s.Description,
                s.SurveyTypeId,
                s.IsActive,
                Questions = s.Questions.OrderBy(q => q.Order).Select(q => new QuestionDto
                {
                    Id = q.id,
                    Text = q.QuestionText,
                    Type = q.QuestionType,
                    IsRequired = q.IsRequired,
                    Options = string.IsNullOrEmpty(q.Options) ? new List<string>() : JsonSerializer.Deserialize<List<string>>(q.Options, (JsonSerializerOptions)null)
                })
            });
        }

        [HttpPut("{id}/questions")]
        public async Task<IActionResult> UpdateQuestions(Guid id, [FromBody] List<QuestionDto> dtos)
        {
            var survey = await _context.Surveys.Include(q => q.Questions).FirstOrDefaultAsync(x => x.id == id);
            if (survey == null) return NotFound();

            using var transaction = _context.Database.BeginTransaction();
            try
            {
                // Delete linked answers first
                var existingQIds = survey.Questions.Select(q => q.id).ToList();
                var linkedAnswers = _context.SurveyAnswers.Where(a => existingQIds.Contains(a.QuestionId));
                _context.SurveyAnswers.RemoveRange(linkedAnswers);
                await _context.SaveChangesAsync();

                // Delete old questions
                _context.SurveyQuestions.RemoveRange(survey.Questions);
                await _context.SaveChangesAsync();

                // Add new questions
                int i = 0;
                foreach (var d in dtos)
                {
                    _context.SurveyQuestions.Add(new SurveyQuestion
                    {
                        SurveyId = id,
                        QuestionText = d.Text,
                        QuestionType = d.Type,
                        IsRequired = d.IsRequired,
                        Order = i++,
                        Options = d.Options != null ? JsonSerializer.Serialize(d.Options) : null
                    });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return Ok(new { message = "Updated" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, ex.Message);
            }
        }
    }

    // --- DTOs ---
    public class SurveySubmissionDto { public Guid SurveyId { get; set; } public Guid? UserId { get; set; } public List<SurveyAnswerDto>? Answers { get; set; } public string? Data { get; set; } }
    public class SurveyAnswerDto { public Guid QuestionId { get; set; } public string? AnswerValue { get; set; } }
    public class CarEntryDto { public Guid Id { get; set; } public string Brand { get; set; } public string Model { get; set; } public int Year { get; set; } public decimal Price { get; set; } }
    
    // USED BY ADMIN & BUILDER
    public class CreateSurveyDto { public string Title { get; set; } = string.Empty; public string? Description { get; set; } public Guid SurveyTypeId { get; set; } public bool IsActive { get; set; } = true; }
    public class QuestionDto { public Guid? Id { get; set; } public string Text { get; set; } = string.Empty; public string Type { get; set; } = "Text"; public bool IsRequired { get; set; } public List<string>? Options { get; set; } }
}