using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MyBGList.DTO;

namespace MyBGList.Controllers.v3
{
    [Route("api/v{version:ApiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("3.0")]
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
                        Name = "Ticket to Ride",
                        Year = 2004,
                        MinPlayers = 2,
                        MaxPlayers = 5
                    },
                    new BoardGame()
                    {
                        Id = 2,
                        Name = "Pandemic",
                        Year = 2008,
                        MinPlayers = 2,
                        MaxPlayers = 4
                    },
                    new BoardGame()
                    {
                        Id = 3,
                        Name = "Scythe",
                        Year = 2016,
                        MinPlayers = 1,
                        MaxPlayers = 7
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
