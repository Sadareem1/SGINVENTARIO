using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SGINVENTARIO.Models;

namespace SGINVENTARIO.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext(options)
{
    public DbSet<Equipo> Equipos => Set<Equipo>();
    public DbSet<Area> Areas => Set<Area>();
    public DbSet<Planta> Plantas => Set<Planta>();
    public DbSet<UsuarioPersona> UsuariosPersona => Set<UsuarioPersona>();
    public DbSet<MovimientoEquipo> Movimientos => Set<MovimientoEquipo>();
    public DbSet<Mantenimiento> Mantenimientos => Set<Mantenimiento>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Equipo>(entity =>
        {
            entity.HasIndex(e => e.CodigoInventario).IsUnique();
            entity.HasIndex(e => e.AreaId);
            entity.HasIndex(e => e.PlantaId);
            entity.HasIndex(e => e.UsuarioAsignadoId);
            entity.Property(e => e.Estado).HasConversion<int>();
            entity.Property(e => e.RowVersion).IsRowVersion();
            entity.Property(e => e.FechaRegistro).HasColumnType("date");
            entity.Property(e => e.GarantiaFin).HasColumnType("date");
        });

        builder.Entity<MovimientoEquipo>(entity =>
        {
            entity.Property(m => m.Tipo).HasConversion<int>();
        });

        builder.Entity<Mantenimiento>(entity =>
        {
            entity.Property(m => m.Tipo).HasConversion<int>();
            entity.Property(m => m.Estado).HasConversion<int>();
        });

        builder.Entity<Area>().HasData(
            new Area { Id = 1, Nombre = "Soporte", Descripcion = "Área de soporte técnico" },
            new Area { Id = 2, Nombre = "Ventas", Descripcion = "Área comercial" },
            new Area { Id = 3, Nombre = "RRHH", Descripcion = "Recursos Humanos" }
        );

        builder.Entity<Planta>().HasData(
            new Planta { Id = 1, Nombre = "Lima", Direccion = "Lima" },
            new Planta { Id = 2, Nombre = "Arequipa", Direccion = "Arequipa" }
        );
    }
}
