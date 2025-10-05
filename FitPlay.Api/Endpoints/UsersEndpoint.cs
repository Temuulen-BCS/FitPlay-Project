using FitPlay.Api.Data;
using FitPlay.Domain.model;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
namespace FitPlay.Api.Endpoints;

public static class UsersEndpoints
{
    public static void MapUsersEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/users", async ([FromServices] ApplicationDbContext db) =>
        {
            var users = await db.Users.ToListAsync();
            return Results.Ok(users);
        })
        .WithName("GetAllUsers")
        .WithOpenApi(op => new(op)
      );

        app.MapGet("/users/{id}", async (int id, [FromServices] ApplicationDbContext db) =>
        {
            var user = await db.Users.FindAsync(id);
            return user is not null ? Results.Ok(user) : Results.NotFound();
        })
        .WithName("GetUserById")
        .WithOpenApi(op => new(op)
       );

        app.MapPost("/users", async (Users user, [FromServices] ApplicationDbContext db) =>
        {
            db.Users.Add(user);
            await db.SaveChangesAsync();
            return Results.Created($"/users/{user.ID}", user);
        })
        .WithName("CreateUser")
        .WithOpenApi(op => new(op)
       );

        app.MapPut("/users/{id}", async (int id, Users inputUser, [FromServices] ApplicationDbContext db) =>
        {
            var user = await db.Users.FindAsync(id);
            if (user is null) return Results.NotFound();

            user.Name = inputUser.Name;
            user.Email = inputUser.Email;
            user.Phone = inputUser.Phone;

            await db.SaveChangesAsync();
            return Results.Ok(user);
        })
        .WithName("UpdateUser")
        .WithOpenApi(op => new(op)
       );

        app.MapDelete("/users/{id}", async (int id, [FromServices] ApplicationDbContext db) =>
        {
            var user = await db.Users.FindAsync(id);
            if (user is null) return Results.NotFound();

            db.Users.Remove(user);
            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithName("DeleteUser")
        .WithOpenApi(op => new(op)
       );
    }
}
