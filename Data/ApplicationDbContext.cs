using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ECommerceBoutique.Models;

namespace ECommerceBoutique.Data
{
    public class ApplicationDbContext : IdentityDbContext<
        User, 
        IdentityRole, 
        string>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<InvoiceItem> InvoiceItems { get; set; }
        public DbSet<Transaction> Transactions { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configuration des propriétés décimales
            builder.Entity<InvoiceItem>()
                .Property(i => i.UnitPrice)
                .HasColumnType("decimal(18,2)");

            builder.Entity<InvoiceItem>()
                .Property(i => i.Subtotal)
                .HasColumnType("decimal(18,2)");

            // Configuration des relations pour éviter les chemins de suppression en cascade multiples
            builder.Entity<Invoice>()
                .HasOne(i => i.Buyer)
                .WithMany()
                .HasForeignKey(i => i.BuyerId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Invoice>()
                .HasOne(i => i.Vendor)
                .WithMany()
                .HasForeignKey(i => i.VendorId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Invoice>()
                .HasOne(i => i.Order)
                .WithMany()
                .HasForeignKey(i => i.OrderId)
                .OnDelete(DeleteBehavior.NoAction);

            // Suppression des tables Identity non utilisées
            builder.Entity<IdentityRoleClaim<string>>().ToTable("NotUsed_RoleClaims");
            builder.Entity<IdentityUserClaim<string>>().ToTable("NotUsed_UserClaims");
            builder.Entity<IdentityUserLogin<string>>().ToTable("NotUsed_UserLogins");
            builder.Entity<IdentityUserToken<string>>().ToTable("NotUsed_UserTokens");

            // Configuration des relations et contraintes
            builder.Entity<Product>()
                .HasOne(p => p.Vendor)
                .WithMany()
                .HasForeignKey(p => p.VendorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Order>()
                .HasOne(o => o.User)
                .WithMany()
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<OrderItem>()
                .HasOne(oi => oi.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<OrderItem>()
                .HasOne(oi => oi.Product)
                .WithMany()
                .HasForeignKey(oi => oi.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Cart>()
                .HasOne(cart => cart.User)
                .WithMany()
                .HasForeignKey(cart => cart.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<CartItem>()
                .HasOne(cartItem => cartItem.Cart)
                .WithMany(cart => cart.Items)
                .HasForeignKey(cartItem => cartItem.CartId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<CartItem>()
                .HasOne(ci => ci.Product)
                .WithMany()
                .HasForeignKey(ci => ci.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Transaction>()
                .HasOne(t => t.Order)
                .WithMany()
                .HasForeignKey(t => t.OrderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Transaction>()
                .HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Transaction>()
                .HasOne(t => t.Invoice)
                .WithMany()
                .HasForeignKey(t => t.InvoiceId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<User>(entity =>
            {
                entity.Property(e => e.Address).IsRequired(false);
                entity.Property(e => e.City).IsRequired(false);
                entity.Property(e => e.PostalCode).IsRequired(false);
                entity.Property(e => e.Country).IsRequired(false);
            });

            // Configuration des propriétés décimales
            builder.Entity<Invoice>()
                .Property(e => e.TotalAmount)
                .HasPrecision(18, 2);

            builder.Entity<Invoice>()
                .Property(e => e.VendorTotal)
                .HasPrecision(18, 2);

            builder.Entity<Order>()
                .Property(e => e.Total)
                .HasPrecision(18, 2);

            builder.Entity<OrderItem>()
                .Property(e => e.UnitPrice)
                .HasPrecision(18, 2);

            builder.Entity<Product>()
                .Property(e => e.Price)
                .HasPrecision(18, 2);

            builder.Entity<Transaction>()
                .Property(e => e.Amount)
                .HasPrecision(18, 2);
        }
    }
}
