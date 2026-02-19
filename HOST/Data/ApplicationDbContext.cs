using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using HOST.Models;
using Microsoft.AspNetCore.Identity;

namespace HOST.Data
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser, IdentityRole, string>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Seating> Seatings { get; set; }
        public DbSet<AssignTableToServer> AssignTableToServers { get; set; }
        public DbSet<QueueEntry> QueueEntries { get; set; }
        public DbSet<Party> Parties { get; set; }
        public DbSet<RestaurantTable> RestaurantTables { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<EmployeeRole> EmployeeRoles { get; set; }
        public DbSet<EmployeeShift> EmployeeShifts { get; set; }
        public DbSet<SystemSetting> SystemSettings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Seating relationships with Employee
            modelBuilder.Entity<Seating>()
                .HasOne(s => s.AssignedServer)
                .WithMany(e => e.AssignedServerEntries)
                .HasForeignKey(s => s.AssignedServerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Seating>()
                .HasOne(s => s.SeatedByEmployee)
                .WithMany(e => e.SeatedByEntries)
                .HasForeignKey(s => s.SeatedByEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
