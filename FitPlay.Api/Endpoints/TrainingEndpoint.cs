using FitPlay.Api.Data;
using FitPlay.Domain.model;
using FitPlay.Domain.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitPlay.Api.Endpoints
{
    public static class TrainingEndpoints
    {
        public static void MapTrainingEndpoints(this IEndpointRouteBuilder app)
        {
            // GET ALL
            app.MapGet("/trainings", async ([FromServices] ApplicationDbContext db) =>
            {
                var trainings = await db.Trainings.ToListAsync();
                return Results.Ok(trainings);
            })
            .WithName("GetAllTrainings")
            .WithOpenApi(op => new(op));

            // GET BY ID
            app.MapGet("/trainings/{id}", async (int id, [FromServices] ApplicationDbContext db) =>
            {
                var training = await db.Trainings.FindAsync(id);
                return training is not null ? Results.Ok(training) : Results.NotFound();
            })
            .WithName("GetTrainingById")
            .WithOpenApi(op => new(op));

            // CREATE
            app.MapPost("/trainings", async (Training training, [FromServices] ApplicationDbContext db) =>
            {
                db.Trainings.Add(training);
                await db.SaveChangesAsync();
                return Results.Created($"/trainings/{training.Id}", training);
            })
            .WithName("CreateTraining")
            .WithOpenApi(op => new(op));

            // UPDATE
            app.MapPut("/trainings/{id}", async (int id, Training input, [FromServices] ApplicationDbContext db) =>
            {
                var training = await db.Trainings.FindAsync(id);
                if (training is null) return Results.NotFound();

                training.Name = input.Name;
                training.Description = input.Description;
                training.Duration = input.Duration;
                training.Points = input.Points;
                training.Athletes = input.Athletes;
                training.MyProperty = input.MyProperty;

                await db.SaveChangesAsync();
                return Results.Ok(training);
            })
            .WithName("UpdateTraining")
            .WithOpenApi(op => new(op));

            // DELETE
            app.MapDelete("/trainings/{id}", async (int id, [FromServices] ApplicationDbContext db) =>
            {
                var training = await db.Trainings.FindAsync(id);
                if (training is null) return Results.NotFound();

                db.Trainings.Remove(training);
                await db.SaveChangesAsync();
                return Results.NoContent();
            })
            .WithName("DeleteTraining")
            .WithOpenApi(op => new(op));
        }
    }
}
