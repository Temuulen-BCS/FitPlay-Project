using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace FitPlay.Api.Auth;

public class ApplicationUser : IdentityUser
{
    [MaxLength(100)]
    public string? FirstName { get; set; }

    [MaxLength(100)]
    public string? LastName { get; set; }
}
