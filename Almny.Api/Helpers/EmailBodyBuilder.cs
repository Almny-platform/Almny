namespace Almny.Api.Helpers;

public static class EmailBodyBuilder
{
    public static string Build(string templateName, Dictionary<string, string> placeholders)
    {
        var templatePath = Path.Combine(
            AppContext.BaseDirectory, "Templates", templateName);

        if (!File.Exists(templatePath))
            throw new FileNotFoundException($"Email template '{templateName}' not found.", templatePath);

        var body = File.ReadAllText(templatePath);

        foreach (var placeholder in placeholders)
        {
            body = body.Replace($"{{{{{placeholder.Key}}}}}", placeholder.Value);
        }

        return body;
    }
}
