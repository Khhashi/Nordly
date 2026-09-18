namespace Nordly.Web;

public static class DatabaseConnectionString
{
    public static string Normalize(string connectionString)
    {
        if (!Uri.TryCreate(connectionString, UriKind.Absolute, out var uri)
            || (uri.Scheme != "postgres" && uri.Scheme != "postgresql"))
        {
            return connectionString;
        }

        var credentials = uri.UserInfo.Split(':', 2);
        var username = Uri.UnescapeDataString(credentials[0]);
        var password = credentials.Length > 1 ? Uri.UnescapeDataString(credentials[1]) : string.Empty;
        var database = Uri.UnescapeDataString(uri.AbsolutePath.Trim('/'));
        var port = uri.Port > 0 ? uri.Port : 5432;

        return $"Host={uri.Host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true";
    }
}