using System.Xml.Linq;

namespace Logiwall.UnitTests.Architecture;

public class LayerDependencyTests
{
    [Theory]
    [InlineData("Shared", "")]
    [InlineData("Domain", "Shared")]
    [InlineData("Application", "Domain,Shared")]
    [InlineData("Infrastructure", "Application,Domain,Shared")]
    public void Project_references_respect_layer_boundaries(string layer, string allowed)
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "instruction.md")))
            root = root.Parent;
        Assert.NotNull(root);
        var project = XDocument.Load(Path.Combine(root.FullName, "src", $"Logiwall.{layer}", $"Logiwall.{layer}.csproj"));
        var references = project.Descendants("ProjectReference")
            .Select(r => Path.GetFileNameWithoutExtension(r.Attribute("Include")!.Value.Replace('\\', '/')))
            .Order().ToArray();
        var expected = allowed.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(name => "Logiwall." + name).Order().ToArray();
        Assert.Equal(expected, references);
    }
}
