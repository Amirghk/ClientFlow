using ClientFlow.Application.Abstractions;
using ClientFlow.Common.Constants;
using ClientFlow.Domain.Identity;
using ClientFlow.Domain.Organizations;
using Microsoft.EntityFrameworkCore;

namespace ClientFlow.Infrastructure;

public sealed class ClientFlowDbContext(DbContextOptions<ClientFlowDbContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<OrganizationMembership> Memberships => Set<OrganizationMembership>();

    async Task IUnitOfWork.SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException) { throw new ClientFlow.Domain.Shared.StaleRecordException(); }
        catch (DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException { SqlState: "23505" or "23503" })
        {
            throw new ClientFlow.Domain.Shared.StaleRecordException();
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ExternalIdentityId).HasMaxLength(255).IsRequired();
            entity.HasIndex(x => x.ExternalIdentityId)
                .HasDatabaseName(DatabaseIndexNames.UsersExternalIdentityId)
                .IsUnique();
        });

        modelBuilder.Entity<Organization>(entity =>
        {
            entity.ToTable("Organizations");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(160).IsRequired();
            entity.HasMany(x => x.Memberships).WithOne().HasForeignKey(x => x.OrganizationId);
            entity.Navigation(x => x.Memberships).HasField("memberships")
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<OrganizationMembership>(entity =>
        {
            entity.ToTable("OrganizationMemberships");
            entity.HasKey(x => new { x.OrganizationId, x.UserId });
            entity.Property(x => x.Role).HasConversion<string>().HasMaxLength(16);
            entity.HasOne<AppUser>().WithMany().HasForeignKey(x => x.UserId);
            entity.HasIndex(x => x.UserId).HasDatabaseName(DatabaseIndexNames.OrganizationMembershipsUserId);
        });

        WorkspaceModel.Configure(modelBuilder);
    }
}
