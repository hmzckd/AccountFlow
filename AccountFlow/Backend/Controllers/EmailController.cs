using AccountFlow.Backend.Models;
using AccountFlow.Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccountFlow.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")] // Locked down: previously an open relay that let anyone send arbitrary mail

    // Manages API endpoints for email operations
    public class EmailController(IEmailService emailService) : ControllerBase
    {
        private readonly IEmailService _emailService = emailService;

        // Accepts email details and delegates the sending process to the service layer
        [HttpPost("sendEmail")]
        public async Task<IActionResult> SendEmail(EmailDto dto)
        {
            if (dto is null) return BadRequest();
            await _emailService.SendMailAsync(dto);
            return NoContent();
        }
    }
}
