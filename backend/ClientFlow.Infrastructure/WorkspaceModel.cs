using ClientFlow.Common.Constants;
using ClientFlow.Domain.Crm;
using ClientFlow.Domain.Identity;
using ClientFlow.Domain.Organizations;
using ClientFlow.Domain.Projects;
using ClientFlow.Domain.Sales;
using ClientFlow.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace ClientFlow.Infrastructure;

internal static class WorkspaceModel
{
    public static void Configure(ModelBuilder model)
    {
        Record(model.Entity<Company>(), "Companies", DatabaseIndexNames.CompaniesTenant);
        Record(model.Entity<Contact>(), "Contacts", DatabaseIndexNames.ContactsTenant);
        Record(model.Entity<Opportunity>(), "Opportunities", DatabaseIndexNames.OpportunitiesTenant);
        Record(model.Entity<Project>(), "Projects", DatabaseIndexNames.ProjectsTenant);
        Record(model.Entity<ProjectTask>(), "ProjectTasks", DatabaseIndexNames.TasksTenant);
        model.Entity<Company>().Property(x => x.Name).HasMaxLength(160);
        model.Entity<Contact>().HasAlternateKey(x => new { x.OrganizationId, x.CompanyId, x.Id });
        model.Entity<Contact>().HasOne<Company>().WithMany().HasForeignKey(x => new { x.OrganizationId, x.CompanyId }).HasPrincipalKey(x => new { x.OrganizationId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        model.Entity<Opportunity>().HasOne<Company>().WithMany().HasForeignKey(x => new { x.OrganizationId, x.CompanyId }).HasPrincipalKey(x => new { x.OrganizationId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        model.Entity<Opportunity>().HasOne<Contact>().WithMany().HasForeignKey(x => new { x.OrganizationId, x.CompanyId, x.ContactId }).HasPrincipalKey(x => new { x.OrganizationId, x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        model.Entity<Opportunity>().Property(x => x.Value).HasPrecision(14, 2);
        model.Entity<Opportunity>().Property(x => x.Currency).HasMaxLength(3);
        model.Entity<Opportunity>().Property(x => x.Stage).HasConversion<string>();
        model.Entity<Project>().HasOne<Company>().WithMany().HasForeignKey(x => new { x.OrganizationId, x.CompanyId }).HasPrincipalKey(x => new { x.OrganizationId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        model.Entity<Project>().HasOne<Opportunity>().WithMany().HasForeignKey(x => new { x.OrganizationId, x.SourceOpportunityId }).HasPrincipalKey(x => new { x.OrganizationId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        model.Entity<Project>().HasIndex(x => new { x.OrganizationId, x.SourceOpportunityId }).IsUnique().HasDatabaseName(DatabaseIndexNames.ProjectSourceOpportunity);
        model.Entity<Project>().Property(x => x.Status).HasConversion<string>();
        model.Entity<ProjectTask>().Property(x => x.Status).HasConversion<string>();
        model.Entity<ProjectTask>().Property(x => x.Priority).HasConversion<string>();
        model.Entity<ProjectTask>().HasOne<Project>().WithMany().HasForeignKey(x => new { x.OrganizationId, x.ProjectId }).HasPrincipalKey(x => new { x.OrganizationId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        model.Entity<ProjectTask>().HasOne<OrganizationMembership>().WithMany().HasForeignKey(x => new { x.OrganizationId, x.AssignedToUserId }).OnDelete(DeleteBehavior.Restrict);
        model.Entity<Organization>().Property(x => x.Version).IsConcurrencyToken();
        model.Entity<AppUser>().Property(x => x.Version).IsConcurrencyToken();
        model.Entity<AppUser>().Property(x => x.DisplayName).HasMaxLength(100);
        model.Entity<Invitation>(e =>
        {
            e.ToTable("Invitations");
            e.HasKey(x => x.Id);
            e.Property(x => x.Version).IsConcurrencyToken();
            e.Property(x => x.TokenHash).HasMaxLength(64);
            e.HasIndex(x => x.TokenHash).IsUnique().HasDatabaseName(DatabaseIndexNames.InvitationToken);
            e.HasOne<Organization>().WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        });
        model.Entity<ActivityEntry>(e =>
        {
            e.ToTable("ActivityEntries");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.OrganizationId, x.CreatedAt }).HasDatabaseName(DatabaseIndexNames.ActivityTenantDate);
            e.HasOne<Organization>().WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        });
        // Preserve UTC timestamp semantics while making date ordering portable in relational tests.
        foreach (var entity in model.Model.GetEntityTypes())
        {
            foreach (var property in entity.GetProperties().Where(x => x.ClrType == typeof(DateTimeOffset)))
            {
                property.SetValueConverter(new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTimeOffset, DateTime>(x => x.UtcDateTime, x => new DateTimeOffset(DateTime.SpecifyKind(x, DateTimeKind.Utc))));
            }

            foreach (var index in entity.GetIndexes())
            {
                var key = entity.ClrType.Name + ":" + string.Join(",", index.Properties.Select(x => x.Name));
                if (DatabaseIndexNames.Relationships.TryGetValue(key, out var name))
                {
                    index.SetDatabaseName(name);
                }
            }
        }
    }
    private static void Record<T>(EntityTypeBuilder<T> entity, string table, string index) where T : WorkspaceRecord
    {
        entity.HasBaseType((Type?)null);
        entity.ToTable(table);
        entity.HasKey(x => x.Id);
        entity.HasAlternateKey(x => new { x.OrganizationId, x.Id });
        entity.Property(x => x.Version).IsConcurrencyToken();
        entity.HasIndex(x => new { x.OrganizationId, x.IsArchived }).HasDatabaseName(index);
        entity.HasOne<Organization>().WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
    }
}
