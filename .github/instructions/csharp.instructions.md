---
description: C# conventions for production and test code
applyTo: "**/*.cs"
---

# C# Conventions

## Test Projects

### File Location and Namespace

Test files mirror the folder structure and namespace of the code under test:

- A test file goes in the same relative folder within the test project.
- Its namespace is the production namespace with a `.Tests` suffix.

```csharp
// Production: MyApp/Wrappers/CommandExceptionHandlerWrapper.cs
namespace MyApp.Wrappers;

// Test: MyApp.Tests/Wrappers/CommandExceptionHandlerWrapperTests.cs
namespace MyApp.Tests.Wrappers;
```

### Test Style

- Name tests `Method_State_Expected`.
- No `// Arrange`, `// Act`, `// Assert` comments; separate the sections with blank lines.

```csharp
[Fact]
public void MyMethod_WhenCondition_ExpectedBehavior()
{
    const string input = "test";

    var result = MyMethod(input);

    Assert.Equal("expected", result);
}
```

### Regions

Group tests in a class with `#region` blocks per member under test:

- `Constructor Tests` for constructors
- `{MethodName} Tests` for methods, for example `Dispose Tests`
- `{PropertyName} Property Tests` for properties, for example `Span Property Tests`
- `Generic Type Tests` for tests of generic type parameter behavior

### xUnit Assertions

Use `Assert.Empty(collection)` for a count of 0 and `Assert.Single(collection)` for 1, not `Assert.Equal(0, collection.Count)`. For larger counts, `Assert.Equal(expectedCount, collection.Count)` is fine.

### Moq

End every mock setup with `.Verifiable()`, and name the lambda parameter `x` in `Setup` and `Verify`:

```csharp
mockService
    .Setup(x => x.DoSomething(It.IsAny<string>()))
    .Returns("result")
    .Verifiable();
```

### InternalsVisibleTo

A project under test grants its tests and Moq access to internal types:

```csharp
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
[assembly: InternalsVisibleTo("MyApp.Tests")]
```

`DynamicProxyGenAssembly2` lets Moq proxy internal types; the second name must match the real test assembly.

## Code Style

### Collection Expressions

Use collection expressions instead of array and collection initializers:

```csharp
byte[] data = [1, 2, 3, 4, 5];
List<string> names = ["Alice", "Bob"];
int[] empty = [];
```

Cast where generic type inference needs it, for example `(byte[])[1, 2, 3]` for a `ReadOnlyMemory<T>` parameter.

### Constants for Literals

Declare literal values with `const`, not `var`:

```csharp
const string expected = "hello";
const int count = 5;
```

### UTF-8 String Literals

Use `u8` literals for byte arrays that hold valid UTF-8 text:

```csharp
var bytes = "Hello"u8.ToArray();
var accented = "café"u8.ToArray();
var emoji = "😀"u8.ToArray();
```

Use explicit byte arrays only for invalid UTF-8 or error-condition tests.

## Code Quality

Fix IDE and analyzer warnings in code you change: use collection expressions, remove redundant type specifications, use `const` for compile-time constants, and make sure every record struct property is read. The build treats warnings as errors.
