using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyBGList.DTO;
using MyBGList.Models;
using System.Linq.Dynamic.Core;

namespace MyBGList.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MechanicsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<MechanicsController> _logger;

        public MechanicsController(ILogger<MechanicsController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        [HttpGet("GetMechanics")]
        [ResponseCache(Location = ResponseCacheLocation.Any, Duration = 60)]
        public async Task<RestDTO<Mechanic[]?>> Get([FromQuery] RequestDTO<MechanicDTO> input)
        {
            // get Queryable object
            var query = _context.Mechanics.AsQueryable();

            // filter
            if (!string.IsNullOrEmpty(input.Filter))
            {
                query = query.Where(x => x.Name.Contains(input.Filter));
            }
            var recordsCount = await query.CountAsync();

            // sort and paginate
            query = query
                .OrderBy($"{input.SortColumn} {input.SortOrder}")
                .Skip(input.PageIndex * input.PageSize)
                .Take(input.PageSize);

            return new RestDTO<Mechanic[]?>
            {
                Data = await query.ToArrayAsync(),
                PageIndex = input.PageIndex,
                PageSize = input.PageSize,
                RecordsCount = recordsCount,
                Links = new List<LinkDTO>
                {   // passing input instead of new { input.PageIndex, input.PageSize } makes more sense for me...
                    new LinkDTO(Url.Action(null, "Mechanics", input, Request.Scheme)!, "self", "GET")
                }
            };
        }

        [HttpPost("UpdateMechanic")]
        [ResponseCache(NoStore = true)]
        public async Task<RestDTO<Mechanic?>> Post(MechanicDTO model)
        {
            var mechanic = await _context.Mechanics
                .Where(x => x.Id == model.Id)
                .FirstOrDefaultAsync();

            if (mechanic != null)
            {
                if (!string.IsNullOrEmpty(model.Name))
                {
                    mechanic.Name = model.Name;
                }
                mechanic.LastModifiedDate = DateTime.Now;

                _context.Mechanics.Update(mechanic);
                await _context.SaveChangesAsync();
            }

            return new RestDTO<Mechanic?>
            {
                Data = mechanic,
                Links = new List<LinkDTO>
                {
                    new LinkDTO(Url.Action(null, "Mechanics", new { mechanic.Id }, Request.Scheme)!, "self", "GET")
                }
            };
        }

        [HttpDelete("DeleteMechanic")]
        [ResponseCache(NoStore = true)]
        public async Task<RestDTO<Mechanic?>> Delete(int id)
        {
            var mechanic = await _context.Mechanics
                .Where(x => x.Id == id)
                .FirstOrDefaultAsync();

            if (mechanic != null)
            {
                _context.Mechanics.Remove(mechanic);
                await _context.SaveChangesAsync();
            }

            return new RestDTO<Mechanic?>
            {
                Data = mechanic,
                Links = new List<LinkDTO>
                {
                    new LinkDTO(Url.Action(null, "Mechanics", new { id }, Request.Scheme)!, "self", "DELETE")
                }
            };
        }
    }
}
