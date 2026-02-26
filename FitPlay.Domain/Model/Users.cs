namespace FitPlay.Domain.Models
{
    public class User
    {
        public int Id { get; set; }    
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;

        /// <summary>
        /// Link to ASP.NET Identity user (ApplicationUser.Id).
        /// </summary>
        public string? IdentityUserId { get; set; }
    }
}
