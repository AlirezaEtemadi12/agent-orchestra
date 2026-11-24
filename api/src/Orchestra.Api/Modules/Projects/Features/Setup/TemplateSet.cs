namespace Orchestra.Api.Modules.Projects.Features.Setup;

/// <summary>Plan 0012 D9: the project templates in <c>engine/templates/project/</c>, copied as bytes.
/// The root folder is passed in, so a test can point it at a temp folder.</summary>
public sealed class TemplateSet
{
    public TemplateSet(string root)
    {
        Root = root;
        Files = Enumerate(root);
    }

    public string Root { get; }

    /// <summary>Relative paths with forward slashes, sorted, under <c>knowledge/</c> left out (those go to
    /// the knowledge rows, not into <c>.orchestra/</c>).</summary>
    public IReadOnlyList<string> Files { get; }

    public bool IsAvailable => Root.Length > 0 && Directory.Exists(Root);

    public byte[] Read(string relative)
    {
        return File.ReadAllBytes(System.IO.Path.Combine(Root, relative.Replace('/', System.IO.Path.DirectorySeparatorChar)));
    }

    public string ReadText(string relative)
    {
        return File.ReadAllText(
            System.IO.Path.Combine(Root, relative.Replace('/', System.IO.Path.DirectorySeparatorChar)),
            System.Text.Encoding.UTF8);
    }

    private static List<string> Enumerate(string root)
    {
        var files = new List<string>();

        if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
        {
            return files;
        }

        foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
        {
            var relative = System.IO.Path.GetRelativePath(root, file).Replace('\\', '/');

            if (relative.StartsWith("knowledge/", StringComparison.Ordinal))
            {
                continue;
            }

            files.Add(relative);
        }

        files.Sort(StringComparer.Ordinal);

        return files;
    }
}
