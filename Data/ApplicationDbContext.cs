using Microsoft.EntityFrameworkCore;
using NetTracApp.Models;

namespace NetTracApp.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<InventoryItem> InventoryItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<InventoryItem>()
                .HasKey(i => i.Id);

            modelBuilder.Entity<InventoryItem>()
                .Property(i => i.Id)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<InventoryItem>()
                .Property(i => i.Vendor)
                .IsRequired()
                .HasMaxLength(255);

            modelBuilder.Entity<InventoryItem>()
                .Property(i => i.DeviceType)
                .HasMaxLength(255);

            modelBuilder.Entity<InventoryItem>()
                .Property(i => i.SerialNumber)
                .HasMaxLength(255);

            modelBuilder.Entity<InventoryItem>()
                .Property(i => i.HostName)
                .HasMaxLength(255);

            modelBuilder.Entity<InventoryItem>()
                .Property(i => i.AssetTag)
                .HasMaxLength(255);

            modelBuilder.Entity<InventoryItem>()
                .Property(i => i.PartID)
                .HasMaxLength(255);

            modelBuilder.Entity<InventoryItem>()
                .Property(i => i.FutureLocation)
                .HasMaxLength(255);

            modelBuilder.Entity<InventoryItem>()
                .Property(i => i.CurrentLocation)
                .HasMaxLength(255);

            modelBuilder.Entity<InventoryItem>()
                .Property(i => i.Status)
                .HasMaxLength(255);

            modelBuilder.Entity<InventoryItem>()
                .Property(i => i.CreatedBy)
                .HasMaxLength(255);

            modelBuilder.Entity<InventoryItem>()
                .Property(i => i.ModifiedBy)
                .HasMaxLength(255);
        }
    }
}
