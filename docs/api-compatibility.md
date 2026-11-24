# Public API compatibility

After its first stable release, the public API of a published package evolves additively. The rules are in
[ADR-0131](decisions/project-template/0131-public-api-is-tracked-and-evolves-compatibly.md),
[ADR-0136](decisions/project-template/0136-packages-follow-semantic-versioning-and-the-assembly-version-moves-only-with-the-major.md),
[ADR-0147](decisions/project-template/0147-packages-are-validated-against-the-last-published-package.md),
and [ADR-0150](decisions/project-template/0150-a-workflow-raises-the-package-validation-baseline-after-each-release.md):

- Add a new overload instead of changing a signature.
- Take required inputs as constructor parameters and optional ones as `init` properties.
- Deprecate with `[Obsolete]`, then remove in the next major version.
- Nullability is part of the signature: changing `string` to `string?` or back is a change to the API.

## What checks it

1. The PublicAPI analyzer, on every build: `RS0016` for a public symbol that is not listed and `RS0017` for a listed symbol that is gone, against `PublicAPI.Shipped.txt` and `PublicAPI.Unshipped.txt`.
2. Package validation (`CP` diagnostics), on every `dotnet pack`: the full `./build/dod.ps1`, CI and `publish.yml`, but not `./build/dod.ps1 -Fast`. It compares each target framework with the others and, once the project sets a baseline, the package with the last published one.

## Changing the public API

Run `./build/Update-PublicApi.ps1` and review the `PublicAPI.Unshipped.txt` diff. A removal is a `*REMOVED*` line, allowed only in a new major version. Raise `version` in `version.json`: the next minor for an addition, the next major for a break.

## Releasing and the baseline

The release change runs `./build/Update-PublicApi.ps1 -Release` and sets `version.json`. The project sets `<PackageValidationBaselineVersion>` once, to the last version on nuget.org, in `Directory.Build.props` after the import or in the packable project's `.csproj`; `.\.template\Update-Project.ps1` and the template's `Adopt-Project.ps1` name that version. From then on `./build/Update-Baseline.ps1 -Version <version>` raises it once nuget.org lists a release, and on GitHub `.github/workflows/raise-baseline.yml` runs it after each release, pushes its branch and opens an issue whose link opens the pull request to merge. A baseline one release behind still compares against a real release and only misses the breaks since then, so a late raise is safe too.

## An intended break

`dotnet pack <project> -p:ApiCompatGenerateSuppressionFile=true` writes `CompatibilitySuppressions.xml`. Review it like the API diff. When `./build/Update-Baseline.ps1` raises the baseline to the first release of the new major version, it removes the baseline suppressions, the entries with `<IsBaselineSuppression>true</IsBaselineSuppression>` and the `PKV006` and `PKV007` entries for a dropped target framework, which then match nothing and which package validation rejects as unnecessary. It keeps any suppression between the package's own target frameworks and deletes the file when nothing is left. Remove them the same way when you raise the baseline by hand.

## Known limit

The pack may print "Could not resolve reference '<assembly>.dll'" for a transitive dependency. It doesn't fail the pack.

Package validation gives each target framework's compile references to ApiCompat, never the runtime-only assemblies. A dependency package that ships a placeholder (`lib/<tfm>/_._`) as its compile asset and its real assembly only at run time, such as `Microsoft.VisualStudio.Threading.Only`, which `Nerdbank.Streams` brings in, is therefore unresolved, once per target framework. Your own code compiled without that assembly, so none of its types can be in your public API (using one is CS0012), and the warning hides no change to your API. Nothing needs fixing.

To silence it anyway, add this to `Directory.Build.props` after the import, with the assembly's path as the package's `lib` file under `$(NuGetPackageRoot)`. The package version and target framework in the path are the ones this example's restore resolved; use the ones your own restore resolved, which `obj/project.assets.json` lists, or the target has no effect and the warning stays. The target hooks the SDK's private `_GetReferencePathFromInnerProjects` target, so check it after an SDK update:

```xml
<Target Name="ResolveRuntimeOnlyReferences" BeforeTargets="RunPackageValidation" DependsOnTargets="_GetReferencePathFromInnerProjects">
  <PropertyGroup>
    <_RuntimeOnlyReference>$(NuGetPackageRoot)microsoft.visualstudio.threading.only/17.13.61/lib/net8.0/Microsoft.VisualStudio.Threading.dll</_RuntimeOnlyReference>
  </PropertyGroup>
  <ItemGroup>
    <PackageValidationReferencePath Update="@(PackageValidationReferencePath)" ReferencePath="%(PackageValidationReferencePath.ReferencePath),$(_RuntimeOnlyReference)" />
  </ItemGroup>
</Target>
```

## Hand-editing PublicAPI files

Prefer the IDE's code fix. The cases that are not obvious:

- Interface variance is not written: list `IFoo<TJob>`, never `IFoo<in TJob>`, or both `RS0016` and `RS0017` fire.
- A non-positional `record class` lists the type and each `.Prop.get` and `.Prop.init`, but no `Type.Type() -> void` constructor line (`RS0017` if present).
- A non-positional `readonly record struct` does list `Type.Type() -> void` plus its properties.
- Compiler-synthesized record members (`Equals`, `GetHashCode`, `ToString`, `Deconstruct`, `PrintMembers`, `with`, operators) are never listed.
- An `extension(Receiver)` block needs three lines: the class, the block marker `Ns.Class.extension(Receiver!)`, then the member line.

The fastest path: write the best guess, build, and let `RS0016` and `RS0017` dictate the text.
