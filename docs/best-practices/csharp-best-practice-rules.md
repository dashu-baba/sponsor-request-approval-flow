# C# Best Practice Rules

A practical rulebook for writing clean, maintainable, production-grade C#.

## Core Rules

### 1. Write code for humans first
Prefer clarity over cleverness. Code is read more often than it is written.

### 2. Use modern C# features intentionally
Use records, pattern matching, nullable reference types, file-scoped namespaces, collection expressions, required properties, and primary constructors when they improve readability.

### 3. Enable nullable reference types
Turn on nullable reference types and handle possible nulls explicitly.

```xml
<Nullable>enable</Nullable>
```

### 4. Avoid `null` as a normal control flow tool
Use empty collections, `TryGet` patterns, `Result` types, optional values, or clear exceptions depending on the situation.

### 5. Use specific exceptions
Catch only exceptions you can actually handle. Do not catch `Exception` everywhere and hide failures.

Bad:

```csharp
try
{
    await ProcessAsync();
}
catch (Exception)
{
    // ignored
}
```

Better:

```csharp
try
{
    await ProcessAsync();
}
catch (TimeoutException ex)
{
    logger.LogWarning(ex, "The operation timed out.");
    throw;
}
```

### 6. Do not use exceptions for normal flow
Exceptions are for exceptional situations. For expected cases, use conditions, validation results, or `Try` methods.

### 7. Use async/await for I/O-bound work
Database calls, HTTP calls, file reads, queue operations, and network operations should be async when async APIs exist.

### 8. Avoid `.Result`, `.Wait()`, and blocking async code
Blocking async code can cause deadlocks, thread starvation, and poor scalability.

### 9. Pass `CancellationToken`
Long-running and I/O-heavy methods should accept and pass cancellation tokens.

```csharp
public Task<User?> GetUserAsync(Guid id, CancellationToken cancellationToken)
{
    return db.Users.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
}
```

### 10. Keep methods small and intention-revealing
A method should do one thing at one level of abstraction. Extract private methods when logic becomes hard to scan.

### 11. Use meaningful names
Names should explain business intent, not just technical type.

Bad:

```csharp
var data = GetData();
```

Better:

```csharp
var activeSubscriptions = GetActiveSubscriptions();
```

### 12. Use `var` only when the type is obvious
Use `var` for clear assignments such as `new User()` or literals. Use explicit types when the right side does not clearly show the type.

### 13. Prefer immutable models where possible
For value-like objects, prefer records or classes with init-only properties.

```csharp
public sealed record Money(decimal Amount, string Currency);
```

### 14. Keep domain logic out of DTOs
DTOs are transport models. Domain rules should live in domain/application services, entities, or value objects.

**Use `record` for DTOs** (and MediatR commands/queries) — they are immutable, value-like data
carriers. Conventions:
- Always `sealed record class` (reference type), never `record struct` for DTOs.
- **Response DTOs:** positional records — `public sealed record RequestSummaryDto(Guid Id, string Title, RequestStatus Status);`
- **Request / command / query DTOs:** prefer init-property records with `required` for readability
  when there are several fields:

```csharp
public sealed record CreateRequestCommand
{
    public required string Title { get; init; }
    public required Guid SponsorshipTypeId { get; init; }
    public decimal RequestedAmount { get; init; }
}
```

- Validation stays external (FluentValidation) — no attribute-vs-positional friction.
- **Exception — multipart/form upload endpoints:** `[FromForm]` binding (with `IFormFile`) doesn't
  bind to positional records; use a plain class or an init-property record there. Pure JSON
  (`[FromBody]`) binds to records fine via System.Text.Json.

### 15. Avoid primitive obsession
When values have business meaning, wrap them in value objects.

```csharp
public sealed record EmailAddress(string Value);
```

### 16. Use dependency injection, not service locators
Inject required dependencies through constructors. Avoid `IServiceProvider.GetService()` inside business logic.

### 17. Avoid static mutable state
Static mutable state is hard to test and dangerous in concurrent applications.

### 18. Use `IEnumerable<T>` for read-only iteration
Expose the least powerful abstraction needed. Do not expose `List<T>` unless callers need list-specific behavior.

### 19. Materialize LINQ intentionally
Understand when LINQ is deferred. Use `ToList()`, `ToArray()`, or async equivalents only when you really need materialized results.

### 20. Avoid multiple enumeration
If an `IEnumerable<T>` will be used multiple times, materialize it once.

### 21. Use `StringBuilder` for large loop-based string construction
String interpolation is great for small strings. For repeated appends inside loops, use `StringBuilder`.

### 22. Use `using` / `await using` for disposable resources
Always dispose streams, database transactions, file handles, and unmanaged resources.

### 23. Keep logging structured
Use message templates, not string interpolation, so logging systems can index properties.

Bad:

```csharp
logger.LogInformation($"User {userId} created order {orderId}");
```

Better:

```csharp
logger.LogInformation("User {UserId} created order {OrderId}", userId, orderId);
```

### 24. Do not log secrets
Never log passwords, tokens, connection strings, private keys, or sensitive personal data.

### 25. Use analyzers and formatting rules
Use `.editorconfig`, Roslyn analyzers, StyleCop/Meziantou analyzers if appropriate, and enforce formatting in CI.

### 26. Prefer composition over inheritance
Use inheritance only when there is a true is-a relationship. Prefer small services and interfaces for behavior composition.

### 27. Keep interfaces meaningful
Do not create an interface for every class automatically. Create interfaces for boundaries, testing seams, plugins, or multiple implementations.

### 28. Test business rules directly
Unit test domain and application logic without needing database, HTTP, or cloud dependencies.

### 29. Keep public APIs stable
Changing public method signatures, DTOs, or contracts should be intentional and versioned when clients depend on them.

### 30. Make invalid states hard to represent
Use constructors, required properties, value objects, enums, and validation to prevent invalid objects from existing.

## Recommended Project Defaults

```xml
<PropertyGroup>
  <TargetFramework>net10.0</TargetFramework>
  <Nullable>enable</Nullable>
  <ImplicitUsings>enable</ImplicitUsings>
  <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
</PropertyGroup>
```

## Senior Rule

Write C# that is explicit at boundaries, simple in the middle, async for I/O, safe around nulls, and boring enough for the next engineer to maintain.
