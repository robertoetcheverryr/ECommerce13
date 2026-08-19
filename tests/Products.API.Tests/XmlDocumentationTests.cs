using System.Reflection;           // For inspecting types, properties and methods at runtime
using System.Xml.Linq;             // For reading the generated Products.API.xml documentation file
using FluentAssertions;
using Products.API.Models;

namespace Products.API.Tests;

/// <summary>
/// Dynamically verifies that public API surface (Controllers, Models, DTOs)
/// has XML documentation. This avoids hardcoding member names so new types
/// without comments make the test fail automatically.
///
/// How it works (high level):
/// 1. The API project is compiled with GenerateDocumentationFile=true
/// 2. That produces Products.API.xml next to the DLL
/// 3. We load that XML and collect every documented member id
/// 4. We reflect over the assembly and require each public member to appear in the XML
///
/// XML member id examples:
///   T:Products.API.Models.Product
///   P:Products.API.Models.Product.Nombre
///   M:Products.API.Controllers.ProductsController.GetById(System.Guid)
/// </summary>
public class XmlDocumentationTests
{
    [Fact]
    public void AllPublicApiMembers_ShouldHaveXmlDocumentation()
    {
        // typeof(Product).Assembly = the Products.API assembly under test
        // (same idea as inspecting a Python module, but for a compiled DLL)
        var assembly = typeof(Product).Assembly;

        // The compiler emits Products.API.xml beside Products.API.dll
        var xmlPath = Path.ChangeExtension(assembly.Location, ".xml");

        File.Exists(xmlPath).Should().BeTrue(
            because: "GenerateDocumentationFile=true should emit Products.API.xml next to the assembly");

        // Load every documented member id from the XML file into a HashSet for O(1) lookups.
        // Example ids: "T:...", "P:...", "M:..."
        var documentedIds = XDocument.Load(xmlPath)
            .Descendants("member")
            .Select(m => m.Attribute("name")?.Value)
            .Where(id => id is not null)
            .Cast<string>()
            .ToHashSet(StringComparer.Ordinal);

        // Only enforce docs on the public API surface that Swagger/clients care about.
        // Exceptions/handlers can be documented too later if we want.
        var namespacesToCheck = new[]
        {
            "Products.API.Controllers",
            "Products.API.Models",
            "Products.API.DTOs"
        };

        var missing = new List<string>();

        // GetExportedTypes() = public types visible outside the assembly
        foreach (var type in assembly.GetExportedTypes()
                     .Where(t => t.Namespace is not null
                                 && namespacesToCheck.Contains(t.Namespace)
                                 && t.IsPublic
                                 && !t.IsNested))
        {
            // --- Type itself (class/record) ---
            // XML id format for types: T:Full.Type.Name
            var typeId = $"T:{type.FullName}";
            if (!documentedIds.Contains(typeId))
                missing.Add(typeId);

            // --- Public instance properties ---
            // DeclaringType == type avoids inherited properties from base classes
            foreach (var prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                         .Where(p => p.DeclaringType == type))
            {
                // XML id format for properties: P:Full.Type.Name.PropertyName
                var propId = $"P:{type.FullName}.{prop.Name}";
                if (!documentedIds.Contains(propId))
                    missing.Add(propId);
            }

            // --- Public methods declared on this type ---
            // IsSpecialName filters out property getters/setters (get_Nombre, set_Nombre, etc.)
            foreach (var method in type.GetMethods(
                         BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                         .Where(m => !m.IsSpecialName))
            {
                // Full XML method ids include parameter types, e.g.:
                //   M:Products.API.Controllers.ProductsController.GetById(System.Guid)
                // Building that string perfectly is verbose, so we accept any documented
                // member that starts with the method prefix.
                var methodPrefix = $"M:{type.FullName}.{method.Name}";
                var found = documentedIds.Any(id =>
                    id.StartsWith(methodPrefix, StringComparison.Ordinal));

                if (!found)
                    missing.Add(methodPrefix);
            }
        }

        // If anything is missing, FluentAssertions prints the full list.
        missing.Should().BeEmpty(
            because: "every public type/property/method in Controllers, Models and DTOs must have XML comments");
    }
}