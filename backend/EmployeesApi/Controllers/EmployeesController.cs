using EmployeesApi.Models;
using EmployeesApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace EmployeesApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmployeesController : ControllerBase
{
    private readonly ICsvParserService _csvParser;
    private readonly IEmployeePairService _employeePairService;

    public EmployeesController(ICsvParserService csvParser, IEmployeePairService employeePairService)
    {
        _csvParser = csvParser;
        _employeePairService = employeePairService;
    }

    [HttpPost("upload")]
    public ActionResult<List<EmployeePairResult>> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "No file provided." });
        }

        try
        {
            using var stream = file.OpenReadStream();
            var records = _csvParser.Parse(stream);

            if (records.Count == 0)
            {
                return BadRequest(new
                {
                    message = "No valid records found. Expected format: EmpID, ProjectID, DateFrom, DateTo"
                });
            }

            var results = _employeePairService.FindLongestCollaboratingPair(records);
            return Ok(results);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Error processing file: {ex.Message}" });
        }
    }
}
