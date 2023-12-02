using Core;
using Core.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Text.RegularExpressions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddCore();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
var factory = app.Services.GetRequiredService<INodeServiceFactory>();
var _address = app.Configuration.GetRequiredSection(WebHostDefaults.ServerUrlsKey).Value!;
var nodeService = factory.GetOrCreateNodeService(_address);
if(_address == "https://localhost:7120")
{
    await nodeService.ConnectToBlockchainAsync(null);
}
else
{
    await nodeService.ConnectToBlockchainAsync(new("https://localhost:7120"));
}

await nodeService.SyncBlocks();

_ = nodeService.MineBlockAsync();

app.Run();
