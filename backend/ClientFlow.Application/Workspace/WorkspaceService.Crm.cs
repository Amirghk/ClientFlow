using ClientFlow.Domain.Crm;
namespace ClientFlow.Application.Workspace;

public sealed partial class WorkspaceService
{
    public async Task<Page<Company>> CompaniesAsync(Guid org, WorkspaceQuery query, CancellationToken ct)
    {
        await Access(org, ct);
        query.Validate();

        return await store.CompaniesAsync(org, query, ct);
    }
    public async Task<Company> CompanyAsync(Guid org, Guid id, CancellationToken ct)
    {
        await Access(org, ct);

        return await RequireCompany(org, id, ct, false);
    }
    public async Task<Company> SaveCompanyAsync(Guid org, Guid? id, CompanyInput input, CancellationToken ct)
    {
        var access = await Access(org, ct);
        var company = id.HasValue ? await RequireCompany(org, id.Value, ct, false) : new Company(org, input.Name);
        company.Update(input.Name, input.Industry, input.Website, input.Email, input.Phone, id.HasValue ? input.Version : company.Version);
        if (!id.HasValue)
        {
            store.AddCompany(company);
        }

        await Commit(access, id.HasValue ? "updated" : "created", "companies", company.Id, company.Name, ct);

        return company;
    }
    public async Task<Page<Contact>> ContactsAsync(Guid org, WorkspaceQuery query, CancellationToken ct)
    {
        await Access(org, ct);
        query.Validate();

        return await store.ContactsAsync(org, query, ct);
    }
    public async Task<Contact> ContactAsync(Guid org, Guid id, CancellationToken ct)
    {
        await Access(org, ct);

        return await store.ContactAsync(org, id, ct) ?? throw new ResourceNotFoundException();
    }
    public async Task<Contact> SaveContactAsync(Guid org, Guid? id, ContactInput input, CancellationToken ct)
    {
        var access = await Access(org, ct);
        var contact = id.HasValue ? await store.ContactAsync(org, id.Value, ct) ?? throw new ResourceNotFoundException() : new Contact(org, input.CompanyId);
        await RequireCompany(org, input.CompanyId, ct, !id.HasValue || input.CompanyId != contact.CompanyId);
        contact.Update(input.CompanyId, input.FirstName, input.LastName, input.Email, input.Phone, input.JobTitle, id.HasValue ? input.Version : contact.Version);
        if (!id.HasValue)
        {
            store.AddContact(contact);
        }

        await Commit(access, id.HasValue ? "updated" : "created", "contacts", contact.Id, $"{contact.FirstName} {contact.LastName}", ct);

        return contact;
    }
}
