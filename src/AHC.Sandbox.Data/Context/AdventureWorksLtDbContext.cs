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

    public DbSet<SalesOrderHeaderEntity> SalesOrderHeaders => Set<SalesOrderHeaderEntity>();

    public DbSet<SalesOrderDetailEntity> SalesOrderDetails => Set<SalesOrderDetailEntity>();

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

        // SalesOrderHeader maps what the read-only Order slice serves. CreditCardApprovalCode is
        // deliberately unmapped — payment data that must never reach the API surface — and
        // RevisionNumber/OnlineOrderFlag/rowguid/ModifiedDate are unmapped for the usual reason:
        // NOT NULL, but all carry database defaults, so their absence can't break an insert.
        // TrackingNumber is a non-standard NOT NULL varchar(18) column real to this database.
        modelBuilder.Entity<SalesOrderHeaderEntity>(entity =>
        {
            entity.ToTable("SalesOrderHeader", "SalesLT");

            entity.HasKey(e => e.SalesOrderId);

            entity.Property(e => e.SalesOrderId)
                .HasColumnName("SalesOrderID");

            // SalesOrderNumber and TotalDue are computed by the database
            // (ISNULL('SO' + CONVERT(...), '*** ERROR ***') and SubTotal + TaxAmt + Freight).
            // ValueGeneratedOnAddOrUpdate tells EF the database owns them: read-only today, and
            // never to be sent in an INSERT/UPDATE if write support ever lands.
            entity.Property(e => e.SalesOrderNumber)
                .HasColumnName("SalesOrderNumber")
                .HasMaxLength(25)
                .IsRequired()
                .ValueGeneratedOnAddOrUpdate();

            entity.Property(e => e.CustomerId)
                .HasColumnName("CustomerID");

            entity.Property(e => e.OrderDate)
                .HasColumnName("OrderDate")
                .HasColumnType("datetime");

            entity.Property(e => e.DueDate)
                .HasColumnName("DueDate")
                .HasColumnType("datetime");

            entity.Property(e => e.ShipDate)
                .HasColumnName("ShipDate")
                .HasColumnType("datetime");

            entity.Property(e => e.Status)
                .HasColumnName("Status");

            entity.Property(e => e.PurchaseOrderNumber)
                .HasColumnName("PurchaseOrderNumber")
                .HasMaxLength(25);

            entity.Property(e => e.AccountNumber)
                .HasColumnName("AccountNumber")
                .HasMaxLength(15);

            entity.Property(e => e.ShipToAddressId)
                .HasColumnName("ShipToAddressID");

            entity.Property(e => e.BillToAddressId)
                .HasColumnName("BillToAddressID");

            entity.Property(e => e.ShipMethod)
                .HasColumnName("ShipMethod")
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.SubTotal)
                .HasColumnName("SubTotal")
                .HasColumnType("money");

            entity.Property(e => e.TaxAmt)
                .HasColumnName("TaxAmt")
                .HasColumnType("money");

            entity.Property(e => e.Freight)
                .HasColumnName("Freight")
                .HasColumnType("money");

            entity.Property(e => e.TotalDue)
                .HasColumnName("TotalDue")
                .HasColumnType("money")
                .ValueGeneratedOnAddOrUpdate();

            entity.Property(e => e.TrackingNumber)
                .HasColumnName("TrackingNumber")
                .HasColumnType("varchar(18)")
                .IsRequired();

            entity.Property(e => e.Comment)
                .HasColumnName("Comment");

            // No back-navigation from detail to header, per the CustomerAddress precedent —
            // nothing reads lines in that direction.
            entity.HasMany(e => e.Details)
                .WithOne()
                .HasForeignKey(d => d.SalesOrderId)
                .IsRequired();
        });

        // rowguid/ModifiedDate unmapped here too (NOT NULL, database defaults).
        modelBuilder.Entity<SalesOrderDetailEntity>(entity =>
        {
            entity.ToTable("SalesOrderDetail", "SalesLT");

            entity.HasKey(e => new { e.SalesOrderId, e.SalesOrderDetailId });

            entity.Property(e => e.SalesOrderId)
                .HasColumnName("SalesOrderID");

            entity.Property(e => e.SalesOrderDetailId)
                .HasColumnName("SalesOrderDetailID")
                .ValueGeneratedOnAdd();

            entity.Property(e => e.OrderQty)
                .HasColumnName("OrderQty");

            entity.Property(e => e.ProductId)
                .HasColumnName("ProductID");

            entity.Property(e => e.UnitPrice)
                .HasColumnName("UnitPrice")
                .HasColumnType("money");

            entity.Property(e => e.UnitPriceDiscount)
                .HasColumnName("UnitPriceDiscount")
                .HasColumnType("money");

            // LineTotal is database-computed: UnitPrice * (1 - UnitPriceDiscount) * OrderQty.
            entity.Property(e => e.LineTotal)
                .HasColumnName("LineTotal")
                .HasColumnType("numeric(38,6)")
                .ValueGeneratedOnAddOrUpdate();
        });
    }
}