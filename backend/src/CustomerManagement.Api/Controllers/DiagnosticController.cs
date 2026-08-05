using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CustomerManagement.Api.Data;

namespace CustomerManagement.Api.Controllers;

// DIAGNOSTIC PROBE C: classic MVC controller action with [FromQuery] binding.
// This source/sink pair is the canonical shape CodeQL's C# ASP.NET Core models
// recognise. If this alerts and the Minimal API probes do not, the gap is
// Minimal API source modelling.
[ApiController]
[Route("api/diagnostic")]
public class DiagnosticController : ControllerBase
{
    private readonly AppDbContext _db;

    public DiagnosticController(AppDbContext db) => _db = db;

    [HttpGet("products")]
    public async Task<IActionResult> Search([FromQuery] string name)
    {
        var conn = _db.Database.GetDbConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Name FROM Products WHERE Name = '" + name + "'";
        using var reader = await cmd.ExecuteReaderAsync();
        var names = new List<string>();
        while (await reader.ReadAsync())
        {
            names.Add(reader.GetString(0));
        }
        return Ok(names);
    }
}
