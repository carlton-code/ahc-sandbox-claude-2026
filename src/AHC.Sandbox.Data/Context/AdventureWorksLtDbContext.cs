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

    public DbSet<AddressEntity> Addresses => Set<AddressEntity>();

    public DbSet<CustomerAddressEntity> CustomerAddresses => Set<CustomerAddressEntity>();

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

        // Address/CustomerAddress map only the columns the read slice needs, following
        // CustomerEntity's precedent. rowguid and ModifiedDate are deliberately unmapped: both
        // are NOT NULL but both have database defaults (newid()/getdate()), so unlike the
        // password columns in ADR-0007 they can't break an insert by being absent.
        modelBuilder.Entity<AddressEntity>(entity =>
        {
            entity.ToTable("Address", "SalesLT");

            entity.HasKey(e => e.AddressId);

            entity.Property(e => e.AddressId)
                .HasColumnName("AddressID");

            entity.Property(e => e.AddressLine1)
                .HasColumnName("AddressLine1")
                .HasMaxLength(60)
                .IsRequired();

            entity.Property(e => e.AddressLine2)
                .HasColumnName("AddressLine2")
                .HasMaxLength(60);

            entity.Property(e => e.City)
                .HasColumnName("City")
                .HasMaxLength(30)
                .IsRequired();

            // StateProvince, CountryRegion and AddressType are the `Name` alias type in this
            // database, not nvarchar directly. EF sees the underlying nvarchar(50).
            entity.Property(e => e.StateProvince)
                .HasColumnName("StateProvince")
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.CountryRegion)
                .HasColumnName("CountryRegion")
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.PostalCode)
                .HasColumnName("PostalCode")
                .HasMaxLength(15)
                .IsRequired();
        });

        modelBuilder.Entity<CustomerAddressEntity>(entity =>
        {
            entity.ToTable("CustomerAddress", "SalesLT");

            entity.HasKey(e => new { e.CustomerId, e.AddressId });

            entity.Property(e => e.CustomerId)
                .HasColumnName("CustomerID");

            entity.Property(e => e.AddressId)
                .HasColumnName("AddressID");

            entity.Property(e => e.AddressType)
                .HasColumnName("AddressType")
                .HasMaxLength(50)
                .IsRequired();

            // WithMany() without a navigation: AddressEntity deliberately has no collection back
            // to CustomerAddress. Nothing reads addresses in that direction, and no address is
            // linked to more than one customer today anyway.
            entity.HasOne(e => e.Address)
                .WithMany()
                .HasForeignKey(e => e.AddressId)
                .IsRequired();
        });
    }
}