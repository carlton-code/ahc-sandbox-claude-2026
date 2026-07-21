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

    public DbSet<ProductCategoryEntity> ProductCategories => Set<ProductCategoryEntity>();

    public DbSet<ProductDescriptionView> ProductDescriptions => Set<ProductDescriptionView>();

    public DbSet<ProductModelEntity> ProductModels => Set<ProductModelEntity>();

    public DbSet<ProductDescriptionEntity> ProductDescriptionRows => Set<ProductDescriptionEntity>();

    public DbSet<ProductModelProductDescriptionEntity> ProductModelProductDescriptions => Set<ProductModelProductDescriptionEntity>();

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

            // IDENTITY column. This is already EF's default for an int key, but pinned explicitly
            // because AddressWriteRepository.CreateForCustomerAsync depends on EF generating the id
            // and fixing it into the CustomerAddress link row — same reason as
            // ProductDescriptionEntity below.
            entity.Property(e => e.AddressId)
                .HasColumnName("AddressID")
                .ValueGeneratedOnAdd();

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
            //
            // ClientNoAction matches the real foreign key, which is NO_ACTION like every other one
            // in this database. Without it EF defaults a required relationship to Cascade, and that
            // default is client-side as well as server-side: deleting an Address while its
            // CustomerAddress happened to be tracked made EF quietly delete the link row too, so the
            // delete succeeded instead of being refused. That contradicts the documented behavior
            // (ADR-0009, ADR-0013) and depended on what else the context had loaded. ClientNoAction
            // leaves tracked dependents alone and lets the database refuse, which is the 547 that
            // DatabaseConflictExceptionHandler turns into a 409.
            entity.HasOne(e => e.Address)
                .WithMany()
                .HasForeignKey(e => e.AddressId)
                .IsRequired()
                .OnDelete(DeleteBehavior.ClientNoAction);
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

        // ProductCategory backs the resolved category on the product read (name + parent name). A
        // self-referencing two-level tree; ParentProductCategoryID is mapped as a plain nullable
        // scalar (no navigation) and the read repository self-joins on it. rowguid/ModifiedDate
        // unmapped as usual (NOT NULL, database defaults).
        modelBuilder.Entity<ProductCategoryEntity>(entity =>
        {
            entity.ToTable("ProductCategory", "SalesLT");

            entity.HasKey(e => e.ProductCategoryId);

            entity.Property(e => e.ProductCategoryId)
                .HasColumnName("ProductCategoryID");

            entity.Property(e => e.ParentProductCategoryId)
                .HasColumnName("ParentProductCategoryID");

            entity.Property(e => e.Name)
                .HasColumnName("Name")
                .HasMaxLength(50)
                .IsRequired();
        });

        // vProductAndDescription is a view, not a table: mapped as a keyless entity
        // (HasNoKey().ToView) so EF treats it as query-only — it can't be tracked or written, which
        // is exactly right for a read-only enrichment. This is the first view mapped in the model;
        // the alternative (raw ADO.NET, as with the unmapped Rewards tables) isn't warranted here
        // because a keyless entity + LINQ expresses the left join fine. Only ProductID/Culture/
        // Description are mapped — Name/ProductModel aren't read. Culture is nchar(6), so its values
        // are space-padded and must be filtered with LIKE 'en%', never = 'en'.
        modelBuilder.Entity<ProductDescriptionView>(entity =>
        {
            entity.HasNoKey();

            entity.ToView("vProductAndDescription", "SalesLT");

            entity.Property(e => e.ProductId)
                .HasColumnName("ProductID");

            entity.Property(e => e.Culture)
                .HasColumnName("Culture")
                .HasColumnType("nchar(6)");

            entity.Property(e => e.Description)
                .HasColumnName("Description")
                .HasMaxLength(400);
        });

        // The description write surface (PUT /product-models/{id}/description) maps the three real
        // tables behind that keyless view, so the edit can be a tracked EF update rather than raw
        // SQL. The read enrichment above still uses the view; these mappings exist for the model
        // resource and its description upsert. rowguid/ModifiedDate are unmapped on all three
        // (NOT NULL with database defaults newid()/getdate()), as elsewhere; ProductModel's
        // CatalogDescription (xml) is unmapped too — nothing reads it.
        modelBuilder.Entity<ProductModelEntity>(entity =>
        {
            entity.ToTable("ProductModel", "SalesLT");

            entity.HasKey(e => e.ProductModelId);

            entity.Property(e => e.ProductModelId)
                .HasColumnName("ProductModelID");

            entity.Property(e => e.Name)
                .HasColumnName("Name")
                .HasMaxLength(50)
                .IsRequired();
        });

        modelBuilder.Entity<ProductDescriptionEntity>(entity =>
        {
            entity.ToTable("ProductDescription", "SalesLT");

            entity.HasKey(e => e.ProductDescriptionId);

            // IDENTITY column — EF generates it on insert (this is EF's default for an int key, but
            // pinned explicitly since the create-description branch depends on it).
            entity.Property(e => e.ProductDescriptionId)
                .HasColumnName("ProductDescriptionID")
                .ValueGeneratedOnAdd();

            entity.Property(e => e.Description)
                .HasColumnName("Description")
                .HasMaxLength(400)
                .IsRequired();
        });

        modelBuilder.Entity<ProductModelProductDescriptionEntity>(entity =>
        {
            entity.ToTable("ProductModelProductDescription", "SalesLT");

            entity.HasKey(e => new { e.ProductModelId, e.ProductDescriptionId, e.Culture });

            entity.Property(e => e.ProductModelId)
                .HasColumnName("ProductModelID");

            entity.Property(e => e.ProductDescriptionId)
                .HasColumnName("ProductDescriptionID");

            // nchar(6), so stored values are space-padded ('en    '). Filter with LIKE 'en%'; a new
            // row inserted with "en" is padded by the database automatically.
            entity.Property(e => e.Culture)
                .HasColumnName("Culture")
                .HasColumnType("nchar(6)");
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