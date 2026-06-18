using Microsoft.EntityFrameworkCore;
using Noter.Domain.Entities.DbEntities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noter.Inrastructure.Persistence.DbContexts
{
    public class NoterDbContext : DbContext
    {
        
        public NoterDbContext(
            DbContextOptions<NoterDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }

        public DbSet<StudyGoal> StudyGoals { get; set; }

        public DbSet<Milestone> Milestones { get; set; }

        public DbSet<StudySessionPlan> StudySessionPlans { get; set; }

        public DbSet<StudySession> StudySessions { get; set; }

        public DbSet<LearningTimer> LearningTimers { get; set; }

        public DbSet<Reminder> Reminders { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasMany(u => u.Goals)
                .WithOne(g => g.User)
                .HasForeignKey(g => g.UserId);

            modelBuilder.Entity<User>()
                .HasMany(u => u.Goals)
                .WithOne()
                .HasForeignKey(s => s.UserId);

            modelBuilder.Entity<StudyGoal>()
                .HasMany(u => u.Milestones)
                .WithOne()
                .HasForeignKey(s => s.StudyGoalId);

            modelBuilder.Entity<StudyGoal>()
                .HasOne(g => g.User)
                .WithMany(u => u.Goals)
                .HasForeignKey(g => g.UserId);

            modelBuilder.Entity<Milestone>()
                .HasMany(m => m.StudySessionPlans)
                .WithOne()
                .HasForeignKey(s => s.MilestoneId);



            //modelBuilder.Entity<MonthlyPlan>()
            //    .HasMany(mp => mp.Milestones)
            //    .WithOne()
            //    .HasForeignKey(m => m.Id);
        }
    }
}
