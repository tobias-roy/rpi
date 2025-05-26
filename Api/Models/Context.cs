using Api.Models;
using Microsoft.EntityFrameworkCore;
namespace Api;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<D1Payload> wemos_data { get; set; }
}