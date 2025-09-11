using Emotions.Application.Common;
using Emotions.Application.DTOs;
using Emotions.Application.DTOs.VoiceRooms;
using Emotions.Application.DTOs.VoiceRooms.Queries;
using Emotions.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Emotions.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VoiceRoomsController : ControllerBase
    {
        private readonly IVoiceRoomService _svc;

        public VoiceRoomsController(IVoiceRoomService svc)
        {
            _svc = svc;
        }


        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<PagedResult<VoiceRoomSummaryDto>>> List([FromQuery] RoomListQuery q,
            CancellationToken ct)
            => Ok(await _svc.ListAsync(q, ct));


        [HttpGet("{id:guid}")]
        [AllowAnonymous]
        public async Task<ActionResult<VoiceRoomDetailDto>> Get(Guid id, CancellationToken ct)
        {
            var dto = await _svc.GetAsync(id, ct);
            return dto is null ? NotFound() : Ok(dto);
        }


        [HttpGet("{id:guid}/participants")]
        [AllowAnonymous]
        public async Task<ActionResult<ParticipantsListDto>> Participants(Guid id, CancellationToken ct)
            => Ok(await _svc.GetParticipantsAsync(id, ct));
    }
}