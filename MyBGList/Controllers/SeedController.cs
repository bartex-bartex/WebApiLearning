using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MyBGList.Models;
using CsvHelper;
using System.Globalization;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using MyBGList.Models.Csv;
using Microsoft.IdentityModel.Tokens;

namespace MyBGList.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class SeedController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SeedController> _logger;
        private readonly IWebHostEnvironment _env;

        public SeedController(ApplicationDbContext context, ILogger<SeedController> logger, IWebHostEnvironment env)
        {
            _context = context;
            _logger = logger;
            _env = env;
        }


        [HttpPut(Name = "Seed")]
        [ResponseCache(NoStore = true)]
        public async Task<IActionResult> Put()
        {
            // Setup
            var config = new CsvConfiguration(CultureInfo.CurrentCulture)
            {
                Delimiter = ";",
                HasHeaderRecord = true
            };

            using (var reader = new StreamReader(Path.Combine(_env.ContentRootPath, "Data/bgg_dataset.csv")))
            using (var csv = new CsvReader(reader, config))
            {
                var existingBoardGames = await _context.BoardGames.ToDictionaryAsync(x => x.Id);
                var existingMechanics = await _context.Mechanics.ToDictionaryAsync(x => x.Name);
                var existingDomains = await _context.Domains.ToDictionaryAsync(x => x.Name);
                var now = DateTime.Now;

                // Execute
                int skippedRows = 0;
                var records = csv.GetRecords<BggRecord>();
                foreach (var record in records)
                {
                    if (!record.ID.HasValue
                        || string.IsNullOrEmpty(record.Name)
                        || existingBoardGames.ContainsKey(record.ID.Value))
                    {
                        skippedRows++;
                        continue;
                    }

                    var boardGame = new BoardGame
                    {
                        Id = record.ID.Value,
                        Name = record.Name,
                        Year = record.YearPublished ?? 0,
                        MinPlayers = record.MinPlayers ?? 0,
                        MaxPlayers = record.MaxPlayers ?? 0,
                        PlayTime = record.PlayTime ?? 0,
                        MinAge = record.MinAge ?? 0,
                        UsersRated = record.UsersRated ?? 0,
                        RatingAverage = record.RatingAverage ?? 0,
                        BGGRank = record.BGGRank ?? 0,
                        ComplexityAverage = record.ComplexityAverage ?? 0,
                        OwnedUsers = record.OwnedUsers ?? 0,
                        CreatedDate = now,
                        LastModifiedDate = now,
                        //BoardGames_Domains = record.BoardGames_Domains ?? new List<BoardGames_Domains>(),
                        //BoardGames_Mechanics = record.BoardGames_Mechanics ?? new List<BoardGames_Mechanics>()
                    };
                    _context.BoardGames.Add(boardGame);

                    if (!string.IsNullOrEmpty(record.Domains))
                    {
                        foreach (var domainName in record.Domains
                            .Split(','))
                        {
                            var domain = existingDomains.GetValueOrDefault(domainName);
                            if (domain == null)
                            {
                                domain = new Domain
                                {
                                    Name = domainName,
                                    CreatedDate = now,
                                    LastModifiedDate = now
                                };
                                _context.Domains.Add(domain);
                                //existingDomains.Add(domainName, domain);

                                _context.BoardGames_Domains.Add(new BoardGames_Domains
                                {
                                    BoardGame = boardGame,
                                    Domain = domain,
                                    CreatedDate = now
                                });
                            }

                        }
                    }

                    if (!string.IsNullOrEmpty(record.Mechanics))
                    {
                        foreach (var mechanicName in record.Mechanics
                            .Split(','))
                        {
                            var mechanic = existingMechanics.GetValueOrDefault(mechanicName);

                            if (mechanic == null)
                            {
                                mechanic = new Mechanic
                                {
                                    Name = mechanicName,
                                    CreatedDate = now,
                                    LastModifiedDate= now
                                };
                                _context.Mechanics.Add(mechanic);
                                //existingMechanics.Add(mechanicName, mechanic);

                                _context.BoardGames_Mechanics.Add(new BoardGames_Mechanics
                                {
                                    BoardGame = boardGame,
                                    Mechanic = mechanic,
                                    CreatedDate = now
                                });
                            }
                        }
                    }
                }

                // save
                using (var transaction = _context.Database.BeginTransaction())
                {
                    _context.Database.ExecuteSqlRaw("SET IDENTITY_INSERT BoardGames ON");
                    await _context.SaveChangesAsync();
                    _context.Database.ExecuteSqlRaw("SET IDENTITY_INSERT BoardGames OFF");
                    transaction.Commit();
                }

                // recap
                return new JsonResult(new
                {
                    BoardGames = _context.BoardGames.Count(),
                    Domains = _context.Domains.Count(),
                    Mechanics = _context.Mechanics.Count(),
                    SkippedRows = skippedRows
                });

            }
        }
    }
}
