using System;
using System.Collections.Generic;

namespace CanteenX.API.Model;

public partial class Menuitem
{
    public int MenuItemId { get; set; }

    public int CanteenId { get; set; }

    public int MenuCategoryId { get; set; }

    public string ItemName { get; set; } = null!;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public string? ImageUrl { get; set; }

    public bool? IsAvailable { get; set; }

    public int? PreparationTime { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Canteen Canteen { get; set; } = null!;

    public virtual Menucategory MenuCategory { get; set; } = null!;
}
