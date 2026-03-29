using LogPath.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LogPath.Api.Data;

public class AppDbContext : DbContext
{
    // Constructor que recibe la conexión a Dokploy
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // Esta será tu tabla en la base de datos
    public DbSet<PosEvent> PosEvents => Set<PosEvent>();
}