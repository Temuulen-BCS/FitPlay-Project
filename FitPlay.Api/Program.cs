using Microsoft.EntityFrameworkCore;
using FitPlay.Api.Data;
using FitPlay.Api.Endpoints;
using System.Security.Claims;
using System.Text.Json;
var builder = WebApplication.CreateBuilder(args);


// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));



var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Map API Endpoints
app.MapBoxEndpoints();
app.MapLevelEndpoints();
app.MapUserEndpoints();
app.MapExerciseEndpoints();
app.MapTrainingEndpoints();



app.Run();
