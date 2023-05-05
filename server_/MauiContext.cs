using Microsoft.EntityFrameworkCore;
using Npgsql;
using server_.Users.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace server_
{
    public class MauiContext : DbContext
    {
        public DbSet<User> Users { get; set; } = null!;
        //public DbSet<DriverUser> Drivers { get; set; } = null!;
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=usersdb;Username=XE;Password=ifvbkm2002");
        }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.HasDefaultSchema("public");
        }
        public MauiContext()
        {
            NpgsqlConnectionStringBuilder builder = new NpgsqlConnectionStringBuilder();
            builder.TrustServerCertificate = true;
            builder.SslMode = SslMode.Require;
            
            
        }
    }
}
