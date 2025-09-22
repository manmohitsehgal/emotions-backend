using Emotions.Application.DTOs.Therapy;
using Emotions.Application.Interfaces;
using Emotions.Domain.Entities;
using Emotions.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;

namespace Emotions.API.Controllers;

[ApiController]
[Route("api/therapy")]
public class TherapyController : ControllerBase
{
    private readonly ITherapyChatService _chat;
    private readonly IUserService _users;
    private readonly AppDbContext _db;

    public TherapyController(ITherapyChatService chat, IUserService users, AppDbContext db)
    {
        _chat = chat;
        _users = users;
        _db = db;
    }

    [HttpPost("conversations")]
    public async Task<ActionResult<ConversationResponse>> CreateConversation([FromBody] CreateConversationRequest req,
        CancellationToken ct)
    {
        var userId = _users.GetAuthenticatedUserId(User);
        var convo = new TherapyConversation
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            IncludeJournal = req.includeJournal,
            IsPremiumSnapshot = User.IsInRole("PremiumUser")
        };
        _db.Add(convo);
        await _db.SaveChangesAsync(ct);
        return new ConversationResponse(convo.Id, convo.IncludeJournal, "Vent", convo.StartedAt);
    }

    [HttpPost("conversations/{id:guid}/messages")]
    public async Task<ActionResult<MessageResponse>> SendMessage(Guid id, [FromBody] SendMessageRequest req,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.text)) return BadRequest("Text is required.");
        var userId = _users.GetAuthenticatedUserId(User);
        var dto = await _chat.SendAsync(id, userId, req.text, ct);
        return Ok(dto);
    }

    [HttpPost("conversations/{id:guid}/summarize")]
    public async Task<ActionResult<SummaryResponse>> Summarize(Guid id, CancellationToken ct)
    {
        var userId = _users.GetAuthenticatedUserId(User);
        var dto = await _chat.SummarizeAsync(id, userId, lastK: 20, ct);
        return Ok(dto);
    }
}