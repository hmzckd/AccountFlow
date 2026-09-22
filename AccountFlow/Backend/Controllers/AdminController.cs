using AccountFlow.Backend.Models;
using AccountFlow.Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccountFlow.Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")] // Restricts access to users with the 'Admin' role

    // Manages administrative operations and statistics
    public class AdminController(IAuthService authService) : ControllerBase
    {
        private readonly IAuthService _authService = authService;

        [HttpGet("stats/daily")]
        public async Task<ActionResult<AdminStatsDto>> GetDailyStats()
        {
            return Ok(await _authService.GetDailyStatsAsync());
        }

        // Retrieves the total number of users registered within the last 24 hours
        [HttpGet("stats/daily-registrations")]
        public async Task<ActionResult<long>> GetUserCount()
        {
            var count = await _authService.GetDailyRegistrationCountAsync();
            return Ok(new { count });
        }

        // Retrieves the number of users who registered in the last 24 hours but have not verified their email
        [HttpGet("stats/daily-unverified")]
        public async Task<ActionResult<long>> GetNotVerifiedUsers()
        {
            var count = await _authService.GetDailyUnverifiedCountAsync();
            return Ok(new { count });
        }
    }
}
