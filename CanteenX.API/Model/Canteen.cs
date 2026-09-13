using System;
using System.Collections.Generic;

namespace CanteenX.API.Model;

public partial class Canteen
{
    public int CanteenId { get; set; }

    public string CanteenName { get; set; } = null!;

    public string? Location { get; set; }

    public string? Description { get; set; }

    public string? ImageUrl { get; set; }

    public bool? IsActive { get; set; }

    public string? UserId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ApplicationUser? User { get; set; }

    public virtual ICollection<Menuitem> Menuitems { get; set; } = new List<Menuitem>();
}
