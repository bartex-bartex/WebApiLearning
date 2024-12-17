using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyBGList.Attributes;
using MyBGList.DTO;
using MyBGList.Models;
using System.ComponentModel.DataAnnotations;
using System.Linq.Dynamic.Core;

namespace MyBGList.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BoardGamesController : ControllerBase
    {
        private readonly ILogger<BoardGamesController> _logger;
        private readonly ApplicationDbContext _context;

        public BoardGamesController(ApplicationDbContext context, ILogger<BoardGamesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("GetBoardGames")]
        [ResponseCache(Location = ResponseCacheLocation.Any, Duration = 60)]
        public async Task<RestDTO<BoardGame[]>> Get([FromQuery] RequestDTO<BoardGameDTO> input)
        {
            _logger.LogInformation(50101, "GetBoardGames action {SortColumn} started", input.SortColumn);

            // This only prepare the query to be executed in DBMS when called .ToArrayAsync()
            var query = _context.BoardGames.AsQueryable();

            // filter
            if (!string.IsNullOrEmpty(input.Filter))
            {
                query = query.Where(x => x.Name.Contains(input.Filter));
            }
            var recordsCount = await query.CountAsync();

            // sort
            query = query
                .OrderBy($"{input.SortColumn} {input.SortOrder}")
                .Skip(input.PageIndex * input.PageSize)
                .Take(input.PageSize);

            _logger.LogCritical(50101, "Returning");

            return new RestDTO<BoardGame[]>
            {
                Data = await query.ToArrayAsync(),
                PageIndex = input.PageIndex,
                PageSize = input.PageSize,
                RecordsCount = recordsCount,
                Links = new List<LinkDTO>
                {
                    new LinkDTO(Url.Action(null, "BoardGames", new { input.PageIndex, input.PageSize }, Request.Scheme)!, "self", "GET")
                }
            };
        }

        [HttpPost("UpdateBoardGame")]
        [ResponseCache(NoStore = true)]
        public async Task<RestDTO<BoardGame?>> Post(BoardGameDTO model)
        {
            var boardGame = await _context.BoardGames
                .Where(x => x.Id == model.Id)
                .FirstOrDefaultAsync();

            if (boardGame != null)
            {
                if (!string.IsNullOrEmpty(model.Name))
                    boardGame.Name = model.Name;

                if (model.Year.HasValue && model.Year.Value > 0)
                    boardGame.Year = model.Year.Value;

                boardGame.LastModifiedDate = DateTime.Now;

                _context.BoardGames.Update(boardGame);
                await _context.SaveChangesAsync();
            }

            return new RestDTO<BoardGame?>
            {
                Data = boardGame,
                Links = new List<LinkDTO>
                {
                    new LinkDTO(Url.Action(
                        null,
                        "BoardGames",
                        model,
                        Request.Scheme)!, "self", "POST")
                }
            };
        }

        [HttpDelete("DeleteBoardGame")]
        [ResponseCache(NoStore = true)]
        public async Task<RestDTO<BoardGame?>> Delete(int id)
        {
            var boardGame = await _context.BoardGames
                .Where(x => x.Id == id)
                .FirstOrDefaultAsync();

            if (boardGame != null)
            {
                _context.BoardGames.Remove(boardGame);
                await _context.SaveChangesAsync();
            }

            return new RestDTO<BoardGame?>
            {
                Data = boardGame,
                Links = new List<LinkDTO>
                {
                    new LinkDTO(
                        Url.Action(
                            null,
                            "BoardGames",
                            id,
                            Request.Scheme)!,
                        "self",
                        "DELETE")
                }
            };
        }
    }
}
