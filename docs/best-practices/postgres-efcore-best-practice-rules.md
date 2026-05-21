# PostgreSQL + EF Core Best Practice Rules

A practical rulebook for using PostgreSQL with EF Core in production applications.

## Core Database Design Rules

### 1. Design the database intentionally
Do not let the ORM accidentally design your database. Think through tables, relationships, constraints, indexes, and query patterns.

### 2. Use proper primary keys
Use stable primary keys. `uuid` is common for distributed systems; `bigint identity` is excellent for high-write internal systems. Choose intentionally.

### 3. Use constraints, not only application validation
Important rules should exist in the database too: `NOT NULL`, `UNIQUE`, `CHECK`, foreign keys, and exclusion constraints where appropriate.

### 4. Use foreign keys for real relationships
Foreign keys protect data integrity. Avoid skipping them just to make writes feel easier.

### 5. Use the right PostgreSQL data types
Use `timestamptz` for real timestamps, `numeric(p,s)` (e.g. `numeric(18,2)`) for money/precise decimal values, `jsonb` for queryable JSON, `text` for variable-length strings, and enums carefully. **Avoid the `money` type** — it is locale-dependent (`lc_monetary`) and has a fixed currency-agnostic scale; use `numeric` instead.

### 6. Store UTC timestamps
Store timestamps in UTC. Convert to user timezone at the application/UI boundary.

### 7. Use soft delete only when the business needs it
Soft delete adds query complexity, indexing complexity, uniqueness issues, and reporting complexity. Use it intentionally.

### 8. Add audit columns consistently
For business systems, use fields like `created_at`, `created_by`, `updated_at`, `updated_by`, and optionally `deleted_at`.

### 9. Name tables, columns, and indexes consistently
Use one convention across the project. For PostgreSQL, snake_case is common.

### 10. Index based on real queries
Indexes should support actual filters, joins, sorting, and uniqueness rules. Do not index every column.

### 11. Understand index cost
Indexes speed up reads but slow down writes and take storage. Every index must earn its place.

### 12. Use composite indexes carefully
Column order matters. Put the most useful equality/filter columns first, then sorting/range columns based on query patterns.

### 13. Use partial indexes when useful
For soft delete, status-based workflows, or active-only records, partial indexes can be very effective.

```sql
CREATE INDEX ix_users_active_email
ON users (email)
WHERE deleted_at IS NULL;
```

### 14. Use database transactions for consistency
When multiple writes must succeed or fail together, use a transaction.

### 15. Keep transactions short
Do not keep transactions open while calling external APIs, uploading files, sending emails, or waiting on users.

### 16. Avoid long locks in migrations
Large table changes can lock production systems. Plan zero-downtime or low-lock migrations for large tables.

## EF Core Query Rules

### 17. Use async queries
Use `ToListAsync`, `FirstOrDefaultAsync`, `SingleOrDefaultAsync`, `SaveChangesAsync`, and pass `CancellationToken`.

### 18. Use `AsNoTracking()` for read-only queries
Tracking has overhead. For read-only screens, reports, and lookup APIs, use no-tracking queries.

```csharp
var users = await db.Users
    .AsNoTracking()
    .Where(x => x.IsActive)
    .Select(x => new UserListItemDto(x.Id, x.Name, x.Email))
    .ToListAsync(cancellationToken);
```

### 19. Project to DTOs instead of loading full entities
Do not load entire entities when the API needs only a few fields.

### 20. Avoid N+1 queries
Be careful with lazy loading and loops that trigger queries repeatedly. Use projection, explicit joins, or carefully chosen `Include`.

Bad:

```csharp
foreach (var order in orders)
{
    Console.WriteLine(order.Customer.Name);
}
```

Better:

```csharp
var orders = await db.Orders
    .AsNoTracking()
    .Select(x => new OrderDto(
        x.Id,
        x.Customer.Name,
        x.Total))
    .ToListAsync(cancellationToken);
```

### 21. Use `Include` only when you need full related entities
For API responses, projection is usually better than `Include`.

### 22. Use pagination for list queries
Never expose unlimited `ToListAsync()` on large tables.

```csharp
var page = await db.Users
    .AsNoTracking()
    .OrderBy(x => x.Id)
    .Skip((pageNumber - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync(cancellationToken);
```

For large datasets, prefer keyset pagination over deep offset pagination.

### 23. Avoid client-side evaluation surprises
Make sure LINQ queries translate to SQL. Watch logs and generated SQL for unexpected behavior.

### 24. Inspect generated SQL
Use logging, `ToQueryString()`, and database query plans when performance matters.

```csharp
var sql = db.Users
    .Where(x => x.Email == email)
    .ToQueryString();
```

### 25. Use compiled queries only for hot paths
Compiled queries can help repeated hot queries, but they add complexity. Measure first.

### 26. Batch writes carefully
For large imports or updates, avoid one `SaveChangesAsync()` per row. Use batching, bulk operations, or raw SQL when appropriate.

### 27. Use concurrency tokens for conflicting updates
For important editable records, use optimistic concurrency so two users do not accidentally overwrite each other.

### 28. Use migrations, but review them
EF migrations are useful, but generated SQL should be reviewed before production deployment.

### 29. Separate migration execution from app startup in production
For serious systems, do not blindly run migrations on app startup. Run them through CI/CD or controlled deployment steps.

### 30. Avoid leaking `DbContext` outside the unit of work
`DbContext` should be scoped to the request/job operation. Do not store it in singletons.

### 31. Keep `DbContext` focused
A giant `DbContext` with hundreds of entities becomes difficult to maintain. For modular monoliths, consider separate contexts by module if needed.

### 32. Use value conversions carefully
Value converters are useful for value objects, enums, and strongly typed IDs, but make sure queries still translate well.

### 33. Use raw SQL when EF is not the right tool
For complex reports, database-specific features, bulk operations, CTE-heavy queries, or performance-critical queries, raw SQL, views, functions, or materialized views can be better.

### 34. Monitor slow queries
Use PostgreSQL logs, `pg_stat_statements`, query plans, and application traces to find slow queries.

### 35. Tune connection pooling
Use sensible pool sizes. More connections are not always better; too many can overload PostgreSQL.

### 36. Use indexes for foreign keys involved in joins
PostgreSQL does not automatically index all foreign key columns. Add indexes when those columns are used in joins, filters, or deletes.

### 37. Avoid storing everything in JSONB
`jsonb` is powerful, but relational data should usually stay relational. Use JSONB for flexible attributes, external payload snapshots, or semi-structured data.

### 38. Use migrations for indexes too
Important indexes should be part of migration history, not manually created in production and forgotten.

### 39. Back up and test restore
A backup that has never been restored is only a hope. Test restore procedures.

### 40. Measure before optimizing
Use real query plans and real production-like data before changing indexes or rewriting queries.

## Recommended EF Core Defaults

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(connectionString);
    options.EnableDetailedErrors(builder.Environment.IsDevelopment());
    options.EnableSensitiveDataLogging(builder.Environment.IsDevelopment());
});
```

## Senior Rule

Let PostgreSQL protect data integrity, let EF Core simplify normal data access, inspect SQL when performance matters, and never treat the database as a dumb storage bucket.
