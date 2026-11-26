using Microsoft.EntityFrameworkCore;
using Api.Models;

namespace Api.Data
{
    public class WhatsappDbContext : DbContext
    {
        public WhatsappDbContext(DbContextOptions<WhatsappDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<Chat> Chats => Set<Chat>();
        public DbSet<ChatMember> ChatMembers => Set<ChatMember>();
        public DbSet<Message> Messages => Set<Message>();
        public DbSet<MessageReaction> MessageReactions => Set<MessageReaction>();
        public DbSet<StarredMessage> StarredMessages => Set<StarredMessage>();
        public DbSet<Status> Statuses => Set<Status>();
        public DbSet<CallLog> CallLogs => Set<CallLog>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ChatMember composite unique constraint
            modelBuilder.Entity<ChatMember>()
                .HasIndex(cm => new { cm.ChatId, cm.UserId })
                .IsUnique();

            // MessageReaction composite unique constraint
            modelBuilder.Entity<MessageReaction>()
                .HasIndex(mr => new { mr.MessageId, mr.UserId })
                .IsUnique();

            // StarredMessage composite unique constraint
            modelBuilder.Entity<StarredMessage>()
                .HasIndex(sm => new { sm.UserId, sm.MessageId })
                .IsUnique();

            // User Sent Messages
            modelBuilder.Entity<Message>()
                .HasOne(m => m.Sender)
                .WithMany(u => u.SentMessages)
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Cascade);

            // Message ReplyTo
            modelBuilder.Entity<Message>()
                .HasOne(m => m.ReplyTo)
                .WithMany()
                .HasForeignKey(m => m.ReplyToId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            // CallLog relations
            modelBuilder.Entity<CallLog>()
                .HasOne(c => c.Caller)
                .WithMany()
                .HasForeignKey(c => c.CallerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CallLog>()
                .HasOne(c => c.Receiver)
                .WithMany()
                .HasForeignKey(c => c.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
