﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyBGList.Attributes;
using MyBGList.Constants;
using MyBGList.DTO;
using MyBGList.Models;
using System.Linq.Dynamic.Core;

namespace MyBGList.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DomainsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DomainsController> _logger;

        public DomainsController(ILogger<DomainsController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        [HttpGet("GetDomains")]
        [ResponseCache(Location = ResponseCacheLocation.Any, Duration = 60)]
        [ManualValidationFilter]
        public async Task<ActionResult<RestDTO<Domain[]?>>> Get([FromQuery] RequestDTO<DomainDTO> input)
        { // Unless you want to return anything else, you can stay with "ActionResult"
            
            if (!ModelState.IsValid)
            {
                var details = new ValidationProblemDetails(ModelState);
                details.Extensions["traceId"] = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier;

                // Check key errors collection
                if (ModelState.Keys.Any(k => k == "PageSize"))
                {
                    details.Type = "https://tools.ietf.org/html/rfc7231#section-6.6.2";
                    details.Status = StatusCodes.Status501NotImplemented;
                    return new ObjectResult(details)
                    {
                        StatusCode = StatusCodes.Status501NotImplemented
                    };
                }
                else
                {
                    details.Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1";
                    details.Status = StatusCodes.Status400BadRequest;
                    return new BadRequestObjectResult(details);
                }
            }

            // get Queryable object
            var query = _context.Domains.AsQueryable();

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

            return new RestDTO<Domain[]?>
            {
                Data = await query.ToArrayAsync(),
                PageIndex = input.PageIndex,
                PageSize = input.PageSize,
                RecordsCount = recordsCount,
                Links = new List<LinkDTO>
                {   // passing input instead of new { input.PageIndex, input.PageSize } makes more sense for me...
                    new LinkDTO(Url.Action(null, "Domains", input, Request.Scheme)!, "self", "GET")
                }
            };
        }

        [HttpPost("UpdateDomain")]
        [ResponseCache(NoStore = true)]
        [Authorize(Roles = RoleNames.Moderator)]
        public async Task<RestDTO<Domain?>> Post(DomainDTO model)
        {
            var domain = await _context.Domains
                .Where(x => x.Id == model.Id)
                .FirstOrDefaultAsync();

            if (domain != null)
            {
                if (!string.IsNullOrEmpty(model.Name))
                {
                    domain.Name = model.Name;
                }
                domain.LastModifiedDate = DateTime.Now;

                _context.Domains.Update(domain);
                await _context.SaveChangesAsync();
            }

            return new RestDTO<Domain?>
            {
                Data = domain,
                Links = new List<LinkDTO>
                {
                    new LinkDTO(Url.Action(null, "Domains", new { domain.Id }, Request.Scheme)!, "self", "GET")
                }
            };
        }

        [HttpDelete("DeleteDomain")]
        [ResponseCache(NoStore = true)]
        [Authorize(Roles = RoleNames.Administrator)]
        public async Task<RestDTO<Domain?>> Delete(int id)
        {
            var domain = await _context.Domains
                .Where(x => x.Id == id)
                .FirstOrDefaultAsync();

            if (domain != null)
            {
                _context.Domains.Remove(domain);
                await _context.SaveChangesAsync();
            }

            return new RestDTO<Domain?>
            {
                Data = domain,
                Links = new List<LinkDTO>
                {
                    new LinkDTO(Url.Action(null, "Domains", new { id }, Request.Scheme)!, "self", "DELETE")
                }
            };
        }
    }
}
