namespace ClientFlow.Domain.Shared;

public sealed class BusinessRuleException(string message) : Exception(message);
public sealed class StaleRecordException() : Exception("This record changed. Reload it before saving again.");

public static class Rules
{
    public static string Text(string? value, string field, int max = 160, bool required = true)
    {
        var text = value?.Trim() ?? string.Empty;
        if ((required && text.Length == 0) || text.Length > max)
        {
            throw new BusinessRuleException($"{field} must contain {(required ? 1 : 0)}–{max} characters.");
        }

        return text;
    }

    public static T EnumValue<T>(string value) where T : struct, Enum
    {
        if (!Enum.GetNames<T>().Contains(value, StringComparer.Ordinal) || !Enum.TryParse<T>(value, false, out var result))
        {
            throw new BusinessRuleException($"Invalid {typeof(T).Name}.");
        }

        return result;
    }

    public static string Email(string? value)
    {
        var email = Text(value, "Email", 254, false);
        if (email.Length > 0 && !System.Net.Mail.MailAddress.TryCreate(email, out _))
        {
            throw new BusinessRuleException("Enter a valid email address.");
        }

        return email;
    }
}

public abstract class WorkspaceRecord
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public Guid OrganizationId
    {
        get; protected set;
    }
    public bool IsArchived
    {
        get; private set;
    }
    public Guid Version { get; private set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;

    public void CheckVersion(Guid version)
    {
        if (version != Version)
        {
            throw new StaleRecordException();
        }
    }

    public void SetArchived(bool archived, Guid version)
    {
        CheckVersion(version);
        IsArchived = archived;
        Touch();
    }

    public void RequireActive()
    {
        if (IsArchived)
        {
            throw new BusinessRuleException("Restore this archived record before using it.");
        }
    }

    protected void Touch()
    {
        Version = Guid.NewGuid();
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
