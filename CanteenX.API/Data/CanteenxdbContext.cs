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

    public virtual DbSet<Menucategory> Menucategories { get; set; }

    public virtual DbSet<Menuitem> Menuitems { get; set; }

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

        modelBuilder.Entity<Menucategory>(entity =>
        {
            entity.HasKey(e => e.MenuCategoryId).HasName("PRIMARY");

            entity.ToTable("menucategories");

            entity.Property(e => e.MenuCategoryId).HasColumnType("int(11)");
            entity.Property(e => e.CategoryName).HasMaxLength(100);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("current_timestamp()")
                .HasColumnType("datetime");
            entity.Property(e => e.Description).HasMaxLength(300);
            entity.Property(e => e.IsActive).HasDefaultValueSql("'1'");
        });

        modelBuilder.Entity<Menuitem>(entity =>
        {
            entity.HasKey(e => e.MenuItemId).HasName("PRIMARY");

            entity.ToTable("menuitems");

            entity.HasIndex(e => e.CanteenId, "CanteenId");

            entity.HasIndex(e => e.MenuCategoryId, "MenuCategoryId");

            entity.Property(e => e.MenuItemId).HasColumnType("int(11)");
            entity.Property(e => e.CanteenId).HasColumnType("int(11)");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("current_timestamp()")
                .HasColumnType("datetime");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.Property(e => e.IsAvailable).HasDefaultValueSql("'1'");
            entity.Property(e => e.ItemName).HasMaxLength(150);
            entity.Property(e => e.MenuCategoryId).HasColumnType("int(11)");
            entity.Property(e => e.PreparationTime).HasColumnType("int(11)");
            entity.Property(e => e.Price).HasPrecision(10, 2);
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("current_timestamp()")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Canteen).WithMany(p => p.Menuitems)
                .HasForeignKey(d => d.CanteenId)
                .HasConstraintName("menuitems_ibfk_1");

            entity.HasOne(d => d.MenuCategory).WithMany(p => p.Menuitems)
                .HasForeignKey(d => d.MenuCategoryId)
                .HasConstraintName("menuitems_ibfk_2");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
