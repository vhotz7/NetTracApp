using Microsoft.EntityFrameworkCore;
using NetTracApp.Models;

namespace NetTracApp.Data
{
    public class ApplicationDbContext : DbContext
    {
        // constructor to initialize the database context with options
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // define a DbSet for InventoryItems to represent the table in the database
        public DbSet<InventoryItem> InventoryItems { get; set; }

        // configure the model properties and relationships
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // specify the primary key for the InventoryItem entity
            modelBuilder.Entity<InventoryItem>()
                .HasKey(i => i.SerialNumber);

            // configure Id property to be auto-generated
            modelBuilder.Entity<InventoryItem>()
                .Property(i => i.SerialNumber)
                .ValueGeneratedOnAdd();

            // set maximum length and required constraint for Vendor property
            modelBuilder.Entity<InventoryItem>()
                .Property(i => i.Vendor)
                .IsRequired()
                .HasMaxLength(255);

            // set maximum length for DeviceType property
            modelBuilder.Entity<InventoryItem>()
                .Property(i => i.DeviceType)
                .HasMaxLength(255);

            // set maximum length for SerialNumber property
            modelBuilder.Entity<InventoryItem>()
                .Property(i => i.SerialNumber)
                .HasMaxLength(255);

            // set maximum length for HostName property
            modelBuilder.Entity<InventoryItem>()
                .Property(i => i.HostName)
                .HasMaxLength(255);

            // set maximum length for AssetTag property
            modelBuilder.Entity<InventoryItem>()
                .Property(i => i.AssetTag)
                .HasMaxLength(255);

            // set maximum length for PartID property
            modelBuilder.Entity<InventoryItem>()
                .Property(i => i.PartID)
                .HasMaxLength(255);

            // set maximum length for FutureLocation property
            modelBuilder.Entity<InventoryItem>()
                .Property(i => i.FutureLocation)
                .HasMaxLength(255);

            // set maximum length for CurrentLocation property
            modelBuilder.Entity<InventoryItem>()
                .Property(i => i.CurrentLocation)
                .HasMaxLength(255);

            // set maximum length for Status property
            modelBuilder.Entity<InventoryItem>()
                .Property(i => i.Status)
                .HasMaxLength(255);

            // set maximum length for CreatedBy property
            modelBuilder.Entity<InventoryItem>()
                .Property(i => i.CreatedBy)
                .HasMaxLength(255);

            // set maximum length for ModifiedBy property
            modelBuilder.Entity<InventoryItem>()
                .Property(i => i.ModifiedBy)
                .HasMaxLength(255);
        }
    }
}
