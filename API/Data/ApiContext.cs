using Microsoft.EntityFrameworkCore;
using API.Models;

namespace API.Data
{
    public class ApiContext : DbContext
    {
        public ApiContext(DbContextOptions<ApiContext> options) : base(options)
        {
        }

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Barco> Barcos { get; set; }
        public DbSet<Empresa_Bodegas> Empresa_Bodegas { get; set; }
        public DbSet<Empresa> Empresas { get; set; }
        public DbSet<Importacion> Importaciones { get; set; }
        public DbSet<Movimiento> Movimientos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Movimiento>()
                .HasOne(m => m.Importacion)
                .WithMany()
                .HasForeignKey(m => m.idimportacion)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Movimiento>()
                .HasOne(m => m.Empresa)
                .WithMany()
                .HasForeignKey(m => m.idempresa)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
