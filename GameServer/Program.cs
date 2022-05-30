using GameServer.Hubs;
using Orleans.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IGameHub, RockPaperScissorsHub>();

// Add services to the container.
builder.Host.UseOrleans(siloBuilder => { siloBuilder.UseLocalhostClustering(); });

builder.Host.ConfigureLogging(logging => { logging.AddConsole(); });

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSignalR();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseRouting();

// app.UseAuthorization();

app.MapHub<RockPaperScissorsHub>("/game");

app.MapControllers();

app.Run();