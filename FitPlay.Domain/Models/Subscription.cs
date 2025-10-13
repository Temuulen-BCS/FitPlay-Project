using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitPlay.Domain.Models;

public class Subscription
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public int TeacherId { get; set; }
    public string Status { get; set; } = "Active"; 
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime? EndDate { get; set; }
    public string? StripeCustomerId { get; set; }
    public string? StripeSubscriptionId { get; set; }
}


