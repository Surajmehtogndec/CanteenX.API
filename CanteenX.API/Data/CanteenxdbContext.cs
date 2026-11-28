using System;
using System.Collections.Generic;
using CanteenX.API.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Scaffolding.Internal;

namespace CanteenX.API.Data;

public partial class CanteenxdbContext : IdentityDbContext<ApplicationUser>
{
    public CanteenxdbContext()
    {
    }

    public CanteenxdbContext(DbContextOptions<CanteenxdbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<CanteenLiveMetric> CanteenLiveMetrics { get; set; }
    public virtual DbSet<CanteenOrder> CanteenOrders { get; set; }
    public virtual DbSet<Canteen> Canteens { get; set; }
    public virtual DbSet<MenuItems> MenuItems { get; set; }
    public virtual DbSet<OrderItems> OrderItems { get; set; }
    public virtual DbSet<Order> Orders { get; set; }
    public virtual DbSet<Payments> Payments { get; set; }
    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }
   

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseMySql("name=ConnectionStrings:DefaultConnection", Microsoft.EntityFrameworkCore.ServerVersion.Parse("10.4.32-mariadb"));

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder
            .UseCollation("utf8mb4_general_ci")
            .HasCharSet("utf8mb4");


        modelBuilder.Entity<CanteenLiveMetric>(entity =>
        {
            entity.HasKey(e => e.MetricId).HasName("PRIMARY");
            entity.ToTable("CanteenLiveMetrics");
            entity.Property(e => e.MetricId).HasColumnType("int(11)");
            entity.Property(e => e.CanteenId).HasColumnType("int(11)");
            entity.Property(e => e.CrowdStatus).HasMaxLength(100);
            entity.Property(e => e.EstimatedWaitTime).HasMaxLength(50);
            entity.Property(e => e.Rating).HasPrecision(3, 2);
            entity.Property(e => e.ReviewCount).HasColumnType("int(11)");
            // Foreign Key Relationship with Canteen
            entity.HasOne(d => d.Canteen)
                .WithMany()
                .HasForeignKey(d => d.CanteenId)
                .HasConstraintName("FK_CanteenLiveMetrics_Canteens_CanteenId")
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CanteenOrder>(entity =>
        {
            entity.HasKey(e => e.CanteenOrderId).HasName("PRIMARY");
            entity.ToTable("CanteenOrders");

            entity.Property(e => e.CanteenOrderId).HasColumnType("int(11)");

            entity.Property(e => e.OrderId).HasColumnType("int(11)");

            entity.Property(e => e.CanteenId).HasColumnType("int(11)");

            entity.Property(e => e.OrderStatus).HasMaxLength(100);
            entity.Property(e => e.Subtotal).HasMaxLength(150);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("current_timestamp()")
                .HasColumnType("datetime");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("current_timestamp()")
                .HasColumnType("datetime");
            // Foreign Key Relationship with Canteen
            entity.HasOne(d => d.Canteen)
                .WithMany()
                .HasForeignKey(d => d.CanteenId)
                .HasConstraintName("FK_CanteenOrders_Canteens_CanteenId")
                .OnDelete(DeleteBehavior.Cascade);
        });


        modelBuilder.Entity<Canteen>(entity =>
        {
            entity.HasKey(e => e.CanteenId).HasName("PRIMARY");

            entity.ToTable("canteens");

            entity.Property(e => e.CanteenId).HasColumnType("int(11)");
            entity.Property(e => e.CanteenName).HasMaxLength(150);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("current_timestamp()")
                .HasColumnType("datetime");

            entity.Property(e => e.Description).HasMaxLength(500);

            entity.Property(e => e.ImageUrl).HasMaxLength(500);

            entity.Property(e => e.IsActive).HasDefaultValueSql("'1'");

            entity.Property(e => e.Location).HasMaxLength(250);

            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("current_timestamp()")
                .HasColumnType("datetime");

            // user canteen relationship
            entity.HasOne(e => e.User)
            .WithOne(e => e.Canteen)
            .HasForeignKey<Canteen>(e => e.UserId)
            .HasConstraintName("FK_Canteens_AspNetUsers_UserId")
            .OnDelete(DeleteBehavior.Restrict);


            // one Identity User = one Canteen

            entity.HasIndex(e => e.UserId)
            .IsUnique();
        });


        modelBuilder.Entity<MenuItems>(entity =>
        {
            entity.HasKey(e => e.MenuItemId).HasName("PRIMARY");

            entity.ToTable("menuitems");

            entity.Property(e => e.MenuItemId).HasColumnType("int(11)");
            entity.Property(e => e.CanteenId).HasColumnType("int(11)");
            entity.Property(e => e.ItemName)
            .HasMaxLength(150) // varchar(150)[cite: 6]
            .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Price)
            .HasMaxLength(500);

            entity.Property(e => e.ImageUrl)
            .HasMaxLength(500) // varchar(500) nullable[cite: 6]
            .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.IsAvailable)
            .HasColumnType("tinyint(1)"); // Default 0 mapped as false[cite: 6]
            

            entity.Property(e => e.CreatedAt)
            .HasColumnType("datetime")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.UpdatedAt)
            .HasColumnType("datetime");

            entity.HasOne(d => d.Canteen)
            .WithMany(p => p.Menuitems)
            .HasForeignKey(d => d.CanteenId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("FK_MenuItems_Canteens");
        });

        modelBuilder.Entity<OrderItems>(entity =>
        {
            entity.HasKey(e => e.OrderItemId).HasName("PRIMARY");
            entity.ToTable("orderitems");

            entity.Property(e => e.OrderItemId)
            .HasColumnType("int(11)")
            .ValueGeneratedOnAdd();

            entity.Property(e => e.CanteenOrderId)
             .HasColumnType("int(11)");

            entity.Property(e => e.ItemName)
               .IsRequired()
               .HasMaxLength(150);


            entity.Property(e => e.UnitPrice)
              .HasMaxLength(500)
              .IsRequired(false);

            entity.Property(e => e.Quantity)
              .HasColumnType("int(11)");

            entity.Property(e => e.SubTotal)
              .HasMaxLength(500)
              .IsRequired(false);

            // CanteenOrder Relationship
            entity.HasOne(e => e.CanteenOrder)
                .WithMany(e => e.OrderItems)
                .HasForeignKey(e => e.CanteenOrderId)
                .OnDelete(DeleteBehavior.Cascade);


            // MenuItem Relationship
            entity.HasOne(e => e.MenuItem)
                .WithMany()
                .HasForeignKey(e => e.MenuItemId)
                .OnDelete(DeleteBehavior.Restrict);

        });


        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.OrderId)
               .HasName("PRIMARY");

            entity.ToTable("orders");


            entity.Property(e => e.OrderId)
              .HasColumnType("int(11)");
            entity.Property(e => e.UserId)
             .IsRequired()
             .HasMaxLength(255);

            entity.Property(e => e.TotalAmount)
             .IsRequired()
             .HasMaxLength(150);
            entity.Property(e => e.OrderStatus)
             .HasMaxLength(30)
             .HasDefaultValue("Pending");
            entity.Property(e => e.PaymentStatus)
             .HasMaxLength(30)
             .HasDefaultValue("Pending");

            entity.Property(e => e.CreatedAt)
              .HasColumnType("datetime")
              .HasDefaultValueSql("current_timestamp()");

            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime");


            // Order -> ApplicationUser
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .HasConstraintName("FK_Orders_AspNetUsers_UserId")
                .OnDelete(DeleteBehavior.Restrict);


            // Order -> CanteenOrders
            entity.HasMany(e => e.CanteenOrders)
                .WithOne(e => e.Order)
                .HasForeignKey(e => e.OrderId)
                .HasConstraintName("FK_CanteenOrders_Orders_OrderId")
                .OnDelete(DeleteBehavior.Cascade);
        });


        modelBuilder.Entity<Payments>(entity =>
        {
            entity.ToTable("payments");

            entity.HasKey(e => e.PaymentId)
             .HasName("PRIMARY");

            // Indexes
            entity.HasIndex(e => e.OrderId, "IX_Payments_OrderId");
            entity.HasIndex(e => e.RazorPayOrderId, "IX_Payments_RazorpayOrderId");
            entity.HasIndex(e => e.RazorpayPaymentId, "IX_Payments_RazorpayPaymentId");
            entity.HasIndex(e => e.PaymentStatus, "IX_Payments_PaymentStatus");


            entity.Property(e => e.PaymentId)
             .HasColumnType("int(11)")
             .ValueGeneratedOnAdd();

            entity.Property(e => e.OrderId)
             .HasColumnType("int(11)");


            entity.Property(e => e.Provider)
             .IsRequired()
             .HasMaxLength(30)
             .HasDefaultValueSql("'Razorpay'");

            entity.Property(e => e.RazorPayOrderId)
            .HasMaxLength(100);

            entity.Property(e => e.RazorpayPaymentId)
             .HasMaxLength(100);

            entity.Property(e => e.Amount)
              .HasColumnType("decimal(10,2)");

            entity.Property(e => e.Currency)
              .IsRequired()
              .HasMaxLength(10)
              .HasDefaultValueSql("'INR'");

            entity.Property(e => e.PaymentStatus)
             .IsRequired()
             .HasMaxLength(30)
             .HasDefaultValueSql("'Created'");

            entity.Property(e => e.PaymentMethod)
              .HasMaxLength(50);

            entity.Property(e => e.CreatedAt)
             .HasColumnType("datetime")
             .HasDefaultValueSql("current_timestamp()");

            entity.Property(e => e.UpdatedAt)
                  .HasColumnType("datetime");

            entity.HasOne(d => d.Order)
            .WithMany(p => p.Payments)
            .HasForeignKey(d => d.OrderId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_Payments_Orders_OrderId");

        });




        modelBuilder.Entity<RefreshToken>(entity => {
            entity.HasKey(e => e.Id).HasName("PRIMARY");
            entity.ToTable("refreshtokens");

            entity.HasIndex(e => e.UserId, "UserId");
            entity.HasIndex(e => e.TokenHash, "TokenHash").IsUnique();

            // Map Id to bigint(20) and AUTO_INCREMENT
            entity.Property(e => e.Id)
            .HasColumnType("bigint(20)")
            .ValueGeneratedOnAdd();

            // Map UserId to varchar(255)
            entity.Property(e => e.UserId)
                .IsRequired()
                .HasMaxLength(255)
                .UseCollation("utf8mb4_general_ci");

            // Map TokenHash to varchar(64)
            entity.Property(e => e.TokenHash)
                .IsRequired()
                .HasMaxLength(64)
                .UseCollation("utf8mb4_general_ci");


            // Map ExpiresAt to datetime
            entity.Property(e => e.ExpiresAt)
                .HasColumnType("datetime");


            // Map CreatedAt to datetime with default current_timestamp()
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("current_timestamp()")
                .HasColumnType("datetime");

            // Nullable RevokedAt
            entity.Property(e => e.RevokedAt)
                .HasColumnType("datetime");

            // Map ReplacedByTokenHash to varchar(64)
            entity.Property(e => e.ReplacedByTokenHash)
                .HasMaxLength(64)
                .UseCollation("utf8mb4_general_ci");


            // Map CreatedByIp to varchar(45)
            entity.Property(e => e.CreatedByIp)
                .HasMaxLength(45)
                .UseCollation("utf8mb4_general_ci");

            // Map RevokedByIp to varchar(45)
            entity.Property(e => e.RevokedByIp)
                .HasMaxLength(45)
                .UseCollation("utf8mb4_general_ci");

            // Foreign Key Relationship with ApplicationUser
            entity.HasOne(d => d.User)
                .WithMany()
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_refreshtokens_AspNetUsers_UserId")
                .OnDelete(DeleteBehavior.Cascade);

        });






        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
