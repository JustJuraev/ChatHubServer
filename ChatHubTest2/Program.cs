using ChatHubTest2.Controllers;
using ChatHubTest2.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();
builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin",
        builder =>
        {
            builder.WithOrigins("*")
                   .AllowAnyHeader()
                   .AllowAnyMethod();
        });
});

builder.Services.AddDbContext<ApplicationContext>(o =>
{
    o.UseNpgsql(builder.Configuration.GetConnectionString("DataBase"));
});
var app = builder.Build();
app.UseCors("AllowSpecificOrigin");
app.UseRouting();
app.MapGet("/", () => "Just try");
app.MapHub<ChatHub>("/chat");
app.MapControllers();

app.Run();
