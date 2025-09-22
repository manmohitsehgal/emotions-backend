using Emotions.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Emotions.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class JournalController : ControllerBase
{
    private readonly IJournalService _journalService;

    public JournalController(IJournalService journalService)
    {
        _journalService = journalService;
    }

    // [HttpPost]
    // public async Task<IActionResult> Create([FromBody] CreateJournalEntryDTO dto)
    // {
    //     var result = await _journalService.CreateAsync(dto);
    //     return Ok(result);
    // }
    //
    // [HttpGet("{userId}")]
    // public async Task<IActionResult> GetAll(string userId)
    // {
    //     var result = await _journalService.GetAllAsync(userId);
    //     return Ok(result);
    // }
}