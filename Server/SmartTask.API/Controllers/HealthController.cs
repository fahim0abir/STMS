using Microsoft.AspNetCore.Mvc;
using SmartTask.Infrastructure.Persistence;

/// <summary>
/// Provides health check endpoints for the API.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    /// <summary>
    /// Checks the health of the API and its database connectivity.
    /// </summary>
    /// <returns>OK if the service and database are reachable.</returns>
    [HttpGet]
    public async Task<ActionResult<string>> CheckAsync([FromServices] ApplicationDbContext db)
    {
        var connected = await db.Database.CanConnectAsync();
        return connected ? Ok("OK") : Problem("Database unavailable", statusCode: 503);
    }
}
