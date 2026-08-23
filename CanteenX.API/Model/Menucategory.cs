using System;
using System.Collections.Generic;

namespace CanteenX.API.Model;

public partial class Menucategory
{
    public int MenuCategoryId { get; set; }

    public string CategoryName { get; set; } = null!;

    public string? Description { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Menuitem> Menuitems { get; set; } = new List<Menuitem>();
}
