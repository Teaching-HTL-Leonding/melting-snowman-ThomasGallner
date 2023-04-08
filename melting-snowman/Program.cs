using System.Collections.Concurrent;
using MeltingSnowman.Logic;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "Melting Snowman API", Version = "v1" });
});
var app = builder.Build();

var nextId = 0;
var games = new ConcurrentDictionary<int, MeltingSnowmanGame>();

app.UseSwagger();
app.UseSwaggerUI();
app.MapGet("/", () => "Melting Snowman");

app.MapGet("/game/{gameId}", (int gameId) =>
{
    if (!games.TryGetValue(gameId, out MeltingSnowmanGame? game))
    {
        return Results.NotFound();
    }

    var resultGame = new Game(game.Word, game.NoOfGuesses);
    return Results.Ok(resultGame);
})
    .Produces<Game>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status404NotFound)
    .WithOpenApi(o =>
    {
        o.Description = "Returns current guessing status.";

        o.Responses[((int)StatusCodes.Status200OK).ToString()].Description = "Success case. A game with the given ID was found";
        o.Responses[((int)StatusCodes.Status404NotFound).ToString()].Description = "No game found with the given ID";

        return o;
    });

app.MapPost("/game", () =>
{
    Interlocked.Increment(ref nextId);
    games.TryAdd(nextId, new MeltingSnowmanGame());

    return Results.Ok(nextId);
})
    .Produces(StatusCodes.Status200OK)
    .WithOpenApi(o =>
    {
        o.Description = "Starts a new game.";

        o.Responses[((int)StatusCodes.Status200OK).ToString()].Description = "A new game was started";

        return o;
    });

app.MapPost("/game/{gameId}", (int gameId, [FromBody] char letter) =>
{
    if (!games.TryGetValue(gameId, out MeltingSnowmanGame? game))
    {
        return Results.NotFound();
    }

    var occurences = game.Guess(letter);

    var guessGame = new GuessGame(occurences, game.Word, game.NoOfGuesses);
    return Results.Ok(guessGame);
})
    .Produces<GuessGame>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status404NotFound)
    .WithOpenApi(o =>
    {
        o.Description = "User tries to guess word with specified letter.";

        o.Responses[((int)StatusCodes.Status200OK).ToString()].Description = "A running game with the given ID was found";
        o.Responses[((int)StatusCodes.Status404NotFound).ToString()].Description = "No game with the given ID was found";

        return o;
    });

app.Run();

record Game(string WordToGuess, int NumberOfGuesses);

record GuessGame(int Occurences, string WordToGuess, int NumberOfGuesses);