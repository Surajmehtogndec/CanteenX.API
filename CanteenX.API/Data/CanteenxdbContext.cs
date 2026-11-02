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

    public virtual DbSet<Canteen> Canteens { get; set; }

    public virtual DbSet<MenuItems> MenuItems { get; set; }
    
    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }
    public virtual DbSet<CanteenLiveMetric> CanteenLiveMetrics { get; set; }


    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseMySql("name=ConnectionStrings:DefaultConnection", Microsoft.EntityFrameworkCore.ServerVersion.Parse("10.4.32-mariadb"));

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder
            .UseCollation("utf8mb4_general_ci")
            .HasCharSet("utf8mb4");

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
            .HasPrecision(10, 2);

            entity.Property(e => e.ImageUrl)
            .HasMaxLength(500) // varchar(500) nullable[cite: 6]
            .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.IsAvailable)
            .HasColumnType("tinyint(1)") // Default 0 mapped as false[cite: 6]
            .HasDefaultValueSql("'1'");

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

      

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
