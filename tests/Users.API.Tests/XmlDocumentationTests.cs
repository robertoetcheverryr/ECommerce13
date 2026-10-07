using System.Reflection;
using System.Xml.Linq;
using FluentAssertions;
using Users.API.DTOs;

namespace Users.API.Tests;

/*
Dynamically verifies that public API surface (Controllers, Models, DTOs)
has XML documentation. This avoids hardcoding member names so new types
without comments make the test fail automatically.

How it works (high level):
1. The API project is compiled with GenerateDocumentationFile=true
2. That produces Users.API.xml next to the DLL
3. We load that XML and collect every documented member id
4. We reflect over the assembly and require each public member to appear in the XML

XML member id examples:
  T:Users.API.DTOs.UserResponse
  P:Users.API.DTOs.UserResponse.Email
  M:Users.API.Controllers.UsersController.Register(Users.API.DTOs.RegisterUserRequest)
*/
public class XmlDocumentationTests
{
    [Fact]
    public void AllPublicApiMembers_ShouldHaveXmlDocumentation()
    {
        var assembly = typeof(UserResponse).Assembly;

        var xmlPath = Path.ChangeExtension(assembly.Location, ".xml");

        File.Exists(xmlPath).Should().BeTrue(
            because: "GenerateDocumentationFile=true should emit Users.API.xml next to the assembly");

        var documentedIds = XDocument.Load(xmlPath)
            .Descendants("member")
            .Select(m => m.Attribute("name")?.Value)
            .Where(id => id is not null)
            .Cast<string>()
            .ToHashSet(StringComparer.Ordinal);

        var namespacesToCheck = new[]
        {
            "Users.API.Controllers",
            "Users.API.Models",
            "Users.API.DTOs"
        };

        var missing = new List<string>();

        foreach (var type in assembly.GetExportedTypes()
                     .Where(t => t.Namespace is not null
                                 && namespacesToCheck.Contains(t.Namespace)
                                 && t.IsPublic
                                 && !t.IsNested))
        {
            var typeId = $"T:{type.FullName}";
            if (!documentedIds.Contains(typeId))
                missing.Add(typeId);

            foreach (var prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                         .Where(p => p.DeclaringType == type))
            {
                var propId = $"P:{type.FullName}.{prop.Name}";
                if (!documentedIds.Contains(propId))
                    missing.Add(propId);
            }

            foreach (var method in type.GetMethods(
                             BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                         .Where(m => !m.IsSpecialName))
            {
                var methodPrefix = $"M:{type.FullName}.{method.Name}";
                var found = documentedIds.Any(id =>
                    id.StartsWith(methodPrefix, StringComparison.Ordinal));

                if (!found)
                    missing.Add(methodPrefix);
            }
        }

        missing.Should().BeEmpty(
            because: "every public type/property/method in Controllers, Models and DTOs must have XML comments");
    }
}
