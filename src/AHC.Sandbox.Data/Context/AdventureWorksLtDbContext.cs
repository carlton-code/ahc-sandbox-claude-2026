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

    public DbSet<ProductEntity> Products => Set<ProductEntity>();

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

        // Product maps the catalog columns only. ThumbNailPhoto/ThumbnailPhotoFileName are
        // deliberately left out (binary payloads don't belong on this API), and rowguid,
        // ModifiedDate and CurrentDiscount are unmapped for the same reason as Address's above:
        // all NOT NULL, but all carry database defaults (newid()/getdate()/0), so their absence
        // can't break an insert. CurrentDiscount is a non-standard column real to this database —
        // don't "correct" it away, and don't map it until a use case needs it.
        modelBuilder.Entity<ProductEntity>(entity =>
        {
            entity.ToTable("Product", "SalesLT");

            entity.HasKey(e => e.ProductId);

            entity.Property(e => e.ProductId)
                .HasColumnName("ProductID");

            // Name is the `Name` alias type in this database, like Address's StateProvince.
            // EF sees the underlying nvarchar(50).
            entity.Property(e => e.Name)
                .HasColumnName("Name")
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.ProductNumber)
                .HasColumnName("ProductNumber")
                .HasMaxLength(25)
                .IsRequired();

            entity.Property(e => e.Color)
                .HasColumnName("Color")
                .HasMaxLength(15);

            // The decimal and datetime columns pin their store types explicitly: these are the
            // first non-string/int mappings in this model, and without them EF picks decimal(18,2)
            // for decimals (warning about silent truncation) and sends DateTime parameters as
            // datetime2 rather than the columns' actual money/decimal(8,2)/datetime types.
            entity.Property(e => e.StandardCost)
                .HasColumnName("StandardCost")
                .HasColumnType("money");

            entity.Property(e => e.ListPrice)
                .HasColumnName("ListPrice")
                .HasColumnType("money");

            entity.Property(e => e.Size)
                .HasColumnName("Size")
                .HasMaxLength(5);

            entity.Property(e => e.Weight)
                .HasColumnName("Weight")
                .HasColumnType("decimal(8,2)");

            entity.Property(e => e.ProductCategoryId)
                .HasColumnName("ProductCategoryID");

            entity.Property(e => e.ProductModelId)
                .HasColumnName("ProductModelID");

            entity.Property(e => e.SellStartDate)
                .HasColumnName("SellStartDate")
                .HasColumnType("datetime");

            entity.Property(e => e.SellEndDate)
                .HasColumnName("SellEndDate")
                .HasColumnType("datetime");

            entity.Property(e => e.DiscontinuedDate)
                .HasColumnName("DiscontinuedDate")
                .HasColumnType("datetime");
        });
    }
}