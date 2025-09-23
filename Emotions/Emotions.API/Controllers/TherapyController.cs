using Emotions.Application.DTOs.Therapy;
using Emotions.Application.Interfaces;
using Emotions.Domain.Entities;
using Emotions.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Emotions.API.Controllers;

[ApiController]
// If your HttpClient base URL already includes "/api", prefer "therapy" here.
// If not, keep "api/therapy".
[Route("api/therapy")]
[Authorize] // remove if you intentionally allow anonymous
public class TherapyController : ControllerBase
{
    private readonly ITherapyChatService _chat;
    private readonly ITherapySummaryService _summary;
    private readonly IUserService _users;
    private readonly AppDbContext _db;

    public TherapyController(
        ITherapyChatService chat,
        ITherapySummaryService summary,
        IUserService users,
        AppDbContext db)
    {
        _chat = chat;
        _summary = summary;
        _users = users;
        _db = db;
    }

    [HttpPost("conversations")]
    [ProducesResponseType(typeof(ConversationResponse), 200)]
    public async Task<ActionResult<ConversationResponse>> CreateConversation(
        [FromBody] CreateConversationRequest req,
        CancellationToken ct = default)
    {
        var userId = _users.GetAuthenticatedUserId(User);

        var convo = new TherapyConversation
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            IncludeJournal = req.includeJournal, // TS sends { includeJournal }
            IsPremiumSnapshot = User.IsInRole("PremiumUser")
        };

        _db.Add(convo);
        await _db.SaveChangesAsync(ct);

        return Ok(new ConversationResponse(convo.Id, convo.IncludeJournal, "Vent", convo.StartedAt));
    }

    [HttpPost("conversations/{id:guid}/messages")]
    [ProducesResponseType(typeof(MessageResponse), 200)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<MessageResponse>> SendMessage(
        Guid id,
        [FromBody] SendMessageRequest req,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(req.text))
            return BadRequest("Text is required.");

        var userId = _users.GetAuthenticatedUserId(User);

        // Delegates to your Chat service that persists the user turn,
        // calls OpenAI, persists the therapist reply, and returns a DTO the RN UI can render.
        var dto = await _chat.SendAsync(id, userId, req.text, ct); // :contentReference[oaicite:0]{index=0}
        return Ok(dto);
    }

    [HttpPost("conversations/{id:guid}/summarize")]
    [ProducesResponseType(typeof(SummaryResponse), 200)]
    public async Task<ActionResult<SummaryResponse>> Summarize(
        Guid id,
        CancellationToken ct = default)
    {
        var userId = _users.GetAuthenticatedUserId(User);

        // Summarizes last K messages using the same ChatClient setup. Returns plain text summary.
        var dto = await _summary.SummarizeAsync(id, userId, lastK: 20, ct); // :contentReference[oaicite:1]{index=1}
        return Ok(dto);
    }
}


// using Emotions.Application.DTOs.Therapy;
// using Emotions.Application.Interfaces;
// using Emotions.Domain.Entities;
// using Emotions.Infrastructure.Data;
// using Microsoft.AspNetCore.Authorization;
// using Microsoft.AspNetCore.Mvc;
//
// namespace Emotions.API.Controllers;
//
// [ApiController]
// // If your HttpClient base URL already includes "/api", prefer "therapy" here.
// // If not, keep "api/therapy".
// [Route("api/therapy")]
// [Authorize] // remove if you intentionally allow anonymous
// public class TherapyController : ControllerBase
// {
//     private readonly ITherapyChatService _chat;
//     private readonly ITherapySummaryService _summary;
//     private readonly IUserService _users;
//     private readonly AppDbContext _db;
//
//     public TherapyController(
//         ITherapyChatService chat,
//         ITherapySummaryService summary,
//         IUserService users,
//         AppDbContext db)
//     {
//         _chat = chat;
//         _summary = summary;
//         _users = users;
//         _db = db;
//     }
//
//     [HttpPost("conversations")]
//     [ProducesResponseType(typeof(ConversationResponse), 200)]
//     public async Task<ActionResult<ConversationResponse>> CreateConversation(
//         [FromBody] CreateConversationRequest req,
//         CancellationToken ct = default)
//     {
//         var userId = _users.GetAuthenticatedUserId(User);
//
//         var convo = new TherapyConversation
//         {
//             Id = Guid.NewGuid(),
//             UserId = userId,
//             IncludeJournal = req.includeJournal, // TS sends { includeJournal }
//             IsPremiumSnapshot = User.IsInRole("PremiumUser")
//         };
//
//         _db.Add(convo);
//         await _db.SaveChangesAsync(ct);
//
//         return Ok(new ConversationResponse(convo.Id, convo.IncludeJournal, "Vent", convo.StartedAt));
//     }
//
//     [HttpPost("conversations/{id:guid}/messages")]
//     [ProducesResponseType(typeof(MessageResponse), 200)]
//     [ProducesResponseType(400)]
//     public async Task<ActionResult<MessageResponse>> SendMessage(
//         Guid id,
//         [FromBody] SendMessageRequest req,
//         CancellationToken ct = default)
//     {
//         if (string.IsNullOrWhiteSpace(req.text))
//             return BadRequest("Text is required.");
//
//         var userId = _users.GetAuthenticatedUserId(User);
//
//         // Delegates to your Chat service that persists the user turn,
//         // calls OpenAI, persists the therapist reply, and returns a DTO the RN UI can render.
//         var dto = await _chat.SendAsync(id, userId, req.text, ct); // :contentReference[oaicite:0]{index=0}
//         return Ok(dto);
//     }
//
//     [HttpPost("conversations/{id:guid}/summarize")]
//     [ProducesResponseType(typeof(SummaryResponse), 200)]
//     public async Task<ActionResult<SummaryResponse>> Summarize(
//         Guid id,
//         CancellationToken ct = default)
//     {
//         var userId = _users.GetAuthenticatedUserId(User);
//
//         // Summarizes last K messages using the same ChatClient setup. Returns plain text summary.
//         var dto = await _summary.SummarizeAsync(id, userId, lastK: 20, ct); // :contentReference[oaicite:1]{index=1}
//         return Ok(dto);
//     }
// }