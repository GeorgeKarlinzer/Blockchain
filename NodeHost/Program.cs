using Core;
using Core.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddCore();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
var nodeService = app.Services.GetRequiredService<INodeService>();
await nodeService.NotifyConnectionService();
_ = nodeService.MineBlock();

app.Run();
