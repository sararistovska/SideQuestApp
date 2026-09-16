using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SideQuestApp.Models; 

namespace SideQuestApp.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Quest> Quests { get; set; }
        public DbSet<QuestCompletion> QuestCompletions { get; set; }
        public DbSet<Badge> Badges { get; set; }
        public DbSet<UserBadge> UserBadges { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<GroupMembership> GroupMemberships { get; set; }
        public DbSet<Friendship> Friendships { get; set; }
        public DbSet<QuestCompletionLike> QuestCompletionLikes { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UserBadge>()
                .HasKey(ub => new { ub.UserId, ub.BadgeId });

            modelBuilder.Entity<UserBadge>()
                .HasOne(ub => ub.User)
                .WithMany(u => u.UserBadges)
                .HasForeignKey(ub => ub.UserId);

            modelBuilder.Entity<UserBadge>()
                .HasOne(ub => ub.Badge)
                .WithMany(b => b.UserBadges)
                .HasForeignKey(ub => ub.BadgeId);

            modelBuilder.Entity<QuestCompletion>()
                .HasOne<Quest>(qc => qc.Quest)
                .WithMany(q => q.Completions)
                .HasForeignKey(qc => qc.QuestId)
                .OnDelete(DeleteBehavior.Restrict);
            
            
            modelBuilder.Entity<GroupMembership>()
                .HasKey(gm => new { gm.UserId, gm.GroupId });

            modelBuilder.Entity<GroupMembership>()
                .HasOne(gm => gm.User)
                .WithMany()
                .HasForeignKey(gm => gm.UserId);

            modelBuilder.Entity<GroupMembership>()
                .HasOne(gm => gm.Group)
                .WithMany(g => g.Memberships)
                .HasForeignKey(gm => gm.GroupId);
            
            modelBuilder.Entity<Group>()
                .HasOne(g => g.CreatedByUser)
                .WithMany()
                .HasForeignKey(g => g.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Quest>()
                .HasOne(q => q.Group)
                .WithMany(g => g.Quests)
                .HasForeignKey(q => q.GroupId)
                .OnDelete(DeleteBehavior.Restrict);
            
            modelBuilder.Entity<Friendship>()
                .HasOne(f => f.Requester)
                .WithMany()
                .HasForeignKey(f => f.RequesterId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Friendship>()
                .HasOne(f => f.Addressee)
                .WithMany()
                .HasForeignKey(f => f.AddresseeId)
                .OnDelete(DeleteBehavior.Restrict);
            
            
            modelBuilder.Entity<QuestCompletionLike>()
                .HasIndex(x => new
                {
                    x.UserId,
                    x.QuestCompletionId
                })
                .IsUnique();

            modelBuilder.Entity<QuestCompletionLike>()
                .HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<QuestCompletionLike>()
                .HasOne(x => x.QuestCompletion)
                .WithMany(x => x.Likes)
                .HasForeignKey(x => x.QuestCompletionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
        
    }
}