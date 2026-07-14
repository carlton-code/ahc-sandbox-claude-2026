using AHC.Sandbox.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AHC.Sandbox.Data.Context;

public class AdventureWorksLtDbContext : DbContext
{
    public AdventureWorksLtDbContext(DbContextOptions<AdventureWorksLtDbContext> options)
        : base(options)
    {
    }

    public DbSet<CustomerEntity> Customers => Set<CustomerEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<CustomerEntity>(entity =>
        {
            entity.ToTable("Customer", "SalesLT");

            entity.HasKey(e => e.CustomerId);

            entity.Property(e => e.CustomerId)
                .HasColumnName("CustomerID");

            entity.Property(e => e.Title)
                .HasMaxLength(8);

            entity.Property(e => e.FirstName)
                .HasColumnName("FirstName")
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.MiddleName)
                .HasColumnName("MiddleName")
                .HasMaxLength(50);

            entity.Property(e => e.LastName)
                .HasColumnName("LastName")
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.CompanyName)
                .HasColumnName("CompanyName")
                .HasMaxLength(128);

            entity.Property(e => e.EmailAddress)
                .HasColumnName("EmailAddress")
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.Phone)
                .HasColumnName("Phone")
                .HasMaxLength(25);
        });
    }
}