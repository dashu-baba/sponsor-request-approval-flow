# ASP.NET Core Best Practice Rules

A practical rulebook for building secure, maintainable, production-ready ASP.NET Core APIs.

## Core Rules

### 1. Keep controllers and endpoints thin
Controllers or Minimal API handlers should receive requests, validate input, call application services, and return responses. Business logic should not live in controllers.

### 2. Use async all the way
Use async APIs for database calls, HTTP calls, file I/O, queues, and external services. Avoid blocking calls such as `.Result`, `.Wait()`, or synchronous stream reads.

### 3. Pass `CancellationToken`
Accept and pass cancellation tokens from HTTP requests to database calls, HTTP clients, and long-running operations.

```csharp
app.MapGet("/users/{id:guid}", async (
    Guid id,
    IUserService users,
    CancellationToken cancellationToken) =>
{
    var user = await users.GetByIdAsync(id, cancellationToken);
    return user is null ? Results.NotFound() : Results.Ok(user);
});
```

### 4. Validate input at the boundary
Validate request DTOs before they enter business logic. Keep transport validation separate from domain rules.

### 5. Use DTOs for API contracts
Do not expose EF Core entities directly from APIs. Use request and response DTOs.

### 6. Return correct HTTP status codes
Use `400` for bad request/validation errors, `401` for unauthenticated, `403` for unauthorized, `404` for not found, `409` for conflicts, and `500` for unexpected errors.

### 7. Use centralized exception handling
Do not put `try/catch` in every endpoint. Use exception middleware and return consistent `ProblemDetails` responses.

### 8. Use Problem Details for API errors
Standardize error responses so frontend, mobile, and external clients can handle errors consistently.

### 9. Keep middleware order intentional
Middleware order matters. A typical order:

```csharp
app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseRouting();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
```

### 10. Configure CORS narrowly
In production, allow only known frontend origins. Do not use permissive CORS unless the API is intentionally public and safe.

### 11. Use authentication and authorization server-side
Never rely only on frontend checks. Protect endpoints with policies, roles, claims, or permission-based authorization.

### 12. Use policy-based authorization for complex rules
For real systems, policies are cleaner than scattered role checks inside controllers.

### 13. Use strongly typed configuration
Bind configuration into option classes and validate them during startup.

```csharp
builder.Services
    .AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection("Jwt"))
    .ValidateDataAnnotations()
    .ValidateOnStart();
```

### 14. Never store secrets in code
Use environment variables, user secrets for local development, Azure Key Vault, AWS Secrets Manager, Docker secrets, or CI/CD secret stores.

### 15. Use `IHttpClientFactory`
Do not manually create new `HttpClient` instances everywhere. Use typed clients or named clients.

### 16. Add health checks
Expose health endpoints for containers, load balancers, and monitoring tools.

```csharp
builder.Services.AddHealthChecks();
app.MapHealthChecks("/health");
```

### 17. Add structured logging
Use `ILogger<T>` with structured properties such as user ID, request ID, correlation ID, entity ID, and operation name.

### 18. Add observability early
Production APIs need logs, metrics, traces, health checks, and alerts. Track latency, error rate, request volume, dependency failures, DB latency, and background job failures.

### 19. Use correlation IDs
Every request should be traceable across API, database, queues, workers, and external services.

### 20. Move long-running work out of requests
Use background workers, queues, hosted services, Hangfire, Quartz, Azure Service Bus, AWS SQS, or similar tools for heavy work.

### 21. Keep hot paths fast
Middleware, authorization handlers, logging enrichers, and filters run frequently. Avoid expensive work in these paths.

### 22. Avoid over-fetching data
Return only the fields needed by the client. Use filtering, pagination, projection, and no-tracking queries for read-only endpoints.

### 23. Add pagination to list endpoints
Never return unlimited database rows from a list endpoint.

### 24. Use idempotency for critical writes
Payment, order creation, workflow approval, file processing, and retryable operations should support idempotency to prevent duplicate work.

### 25. Use transactions carefully
Keep transactions short. Do not call external APIs while holding a database transaction.

### 26. Version public APIs
Use API versioning when external clients depend on your contract.

### 27. Secure Swagger/OpenAPI in production
Swagger is useful, but production access should be restricted, authenticated, or disabled based on the system.

### 28. Use HTTPS and secure headers
Force HTTPS, use HSTS where appropriate, and configure security headers through middleware or reverse proxy.

### 29. Keep packages and runtime patched
Regularly patch .NET runtime, ASP.NET Core packages, container base images, and reverse proxies.

### 30. Prefer modular monolith before microservices
Start with clear modules and boundaries inside one deployable application. Move to microservices only when organizational, scaling, or domain complexity justifies it.

### 31. Stream file uploads — never buffer them into memory
Do not read uploads into a `byte[]` or `MemoryStream` (`ReadAllBytes`, `ToArray()`) — buffers over
~85 KB land on the Large Object Heap and create avoidable GC pressure. Stream the request straight
to storage:

- Stream `IFormFile.OpenReadStream()` **directly** to object storage
  (`PutObjectRequest { InputStream = stream, AutoCloseStream = true }` or `TransferUtility.UploadAsync`).
  `IFormFile` spills sections above the 64 KB threshold to a temp file (not the managed heap), so it
  stays GC-friendly as long as you never materialize the bytes.
- `Stream.CopyToAsync` uses an 80 KB (sub-LOH) buffer by default; if you must hand-copy, rent from
  `ArrayPool<byte>`.
- Bound memory and abuse with explicit size limits: `MultipartBodyLengthLimit` + Kestrel request-body
  limit + reverse-proxy `client_max_body_size`. Validate content-type/extension and reject early.
- For large/unbounded files, escalate to `MultipartReader` + `[DisableFormValueModelBinding]` and
  stream multipart sections directly — this avoids buffering and form-binding overhead entirely.

## Recommended Project Structure

```txt
src/
  MyApp.Api/
  MyApp.Application/
  MyApp.Domain/
  MyApp.Infrastructure/

tests/
  MyApp.UnitTests/
  MyApp.IntegrationTests/
```

## Senior Rule

Keep the API layer thin, business rules isolated, infrastructure replaceable, errors consistent, and production behavior observable.
