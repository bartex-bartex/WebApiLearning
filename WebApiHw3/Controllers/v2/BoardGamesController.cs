using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MyBGList.DTO;
using System.ComponentModel.DataAnnotations;

namespace MyBGList.Controllers.v2
{
    [Route("api/v{version:ApiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("2.0")]
    [ResponseCache(Duration=30, Location=ResponseCacheLocation.Client)]
    public class BoardGamesController : ControllerBase
    {
        private readonly ILogger<BoardGamesController> _logger;

        public BoardGamesController(ILogger<BoardGamesController> logger)
        {
            _logger = logger;
        }

        [HttpGet(Name = "GetBoardGames")]
        public RestDTO<BoardGame[]> Get()
        {
            return new RestDTO<BoardGame[]>()
            {
                Data = new BoardGame[]
                {
                    new BoardGame()
                    {
                        Id = 1,
                        Name = "Catan",
                        Year = 1995,
                        MinPlayers = 3,
                        MaxPlayers = 4
                    },
                    new BoardGame()
                    {
                        Id = 2,
                        Name = "Carcassonne",
                        Year = 2000,
                        MinPlayers = 2,
                        MaxPlayers = 6
                    },
                    new BoardGame()
                    {
                        Id = 3,
                        Name = "Gloomhaven",
                        Year = 2017,
                        MinPlayers = 1,
                        MaxPlayers = 4
                    }
                },
                Links = new List<LinkDTO>()
                {
                    new LinkDTO(Url.Action(null, "BoardGames", null, Request.Scheme)!, "self", "GET")
                }
            };
        }
    }
}
