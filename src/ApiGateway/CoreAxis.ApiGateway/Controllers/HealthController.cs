using CoreAxis.Modules.AuthModule.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoreAxis.ApiGateway.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly AuthDbContext _dbContext;

        public HealthController(AuthDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet("test-sql")]
        public async Task<IActionResult> TestSqlConnection()
        {
            var canConnect = await _dbContext.Database.CanConnectAsync();
            return canConnect ? Ok("Connection successful!") : StatusCode(500, "Connection failed!");
        }
    }
}