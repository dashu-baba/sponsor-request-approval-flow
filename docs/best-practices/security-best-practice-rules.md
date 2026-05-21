# Security Best Practice Rules

A practical rulebook for building secure web applications (auth, OWASP, secrets, uploads).

## Authentication & Authorization

### 1. Authenticate and authorize on the server
Never trust the client. Every protected endpoint enforces authN + authZ server-side; frontend
checks are UX only, not security.

### 2. Enforce authorization at two layers
Gate routes with role/policy attributes **and** re-check ownership/resource rules inside handlers
(e.g. a requestor only acts on their own request; no self-approval).

### 3. Hash passwords with a strong adaptive algorithm
Use the framework identity hasher (PBKDF2/bcrypt/argon2). Never store plaintext or fast hashes
(MD5/SHA-1). Never roll your own.

### 4. Use short-lived access tokens + safe refresh
Keep JWT access tokens short-lived. Store refresh tokens in `HttpOnly`, `Secure`, `SameSite`
cookies — never in browser `localStorage`/`sessionStorage` (XSS-exfiltratable).

### 5. Sign and validate tokens correctly
Validate issuer, audience, lifetime, and signature. Use a strong secret/asymmetric key from
config, not source. Reject `alg: none`.

### 6. Apply least privilege
Each role gets the minimum permissions needed. Default-deny: an endpoint without an explicit
allow is denied.

## Input, Output & Injection

### 7. Validate all input at the boundary
Validate type, length, range, and format before business logic. Treat all client input as hostile.

### 8. Use parameterized queries only
Use the ORM or parameters; never concatenate user input into SQL/commands. No string-built queries.

### 9. Encode output to prevent XSS
Rely on the framework's default escaping. Avoid rendering raw, unescaped HTML from user/external
content; if rich text is unavoidable, sanitize it server-side with a vetted sanitizer first.

### 10. Prevent mass assignment / over-posting
Bind to explicit request DTOs, not domain entities. Never let clients set server-owned fields
(status, owner, role, timestamps).

### 11. Protect against CSRF where relevant
For cookie-based auth, use anti-CSRF tokens or `SameSite` cookies + origin checks.

## Files, CORS & Headers

### 12. Validate uploads strictly
Check content type and extension, enforce a max size, generate server-side storage keys (never
trust client filenames/paths), and store outside the web root (object storage). Stream large files.

### 13. Lock down CORS
Allow only known frontend origins with explicit methods/headers. Never reflect arbitrary origins
or use `*` with credentials.

### 14. Send security headers + force HTTPS
Force HTTPS/HSTS; set `X-Content-Type-Options: nosniff`, a sensible `Content-Security-Policy`,
and frame-ancestors/`X-Frame-Options`. Terminate TLS at the proxy.

### 15. Rate-limit and throttle
Apply rate limiting to auth and write-heavy endpoints to blunt brute-force and abuse.

## Secrets, Errors & Dependencies

### 16. Keep secrets out of source
No secrets in code, config, or git history. Use env vars / user-secrets / a secrets manager.
Commit only `.env.example` with placeholder values.

### 17. Fail securely; don't leak internals
Return generic errors via `ProblemDetails`. Never expose stack traces, SQL, or framework versions
to clients. Log details server-side.

### 18. Don't log sensitive data
Never log passwords, tokens, cookies, connection strings, or PII. Redact before logging.

### 19. Keep dependencies patched
Track and update vulnerable packages and base images. Run dependency scanning in CI.

### 20. Audit security-relevant actions
Record who did what and when for sensitive operations (logins, approvals, role changes,
data exports) in an immutable trail.

## OWASP Anchor
Map work against the OWASP Top 10 (Broken Access Control, Injection, Auth failures, Security
Misconfiguration, Vulnerable Components, etc.). Access-control bugs are the most common — review
authZ on every endpoint.

## Senior Rule

Assume every request is hostile, enforce authorization server-side at every layer, keep secrets
and internals invisible, and make security-relevant actions auditable.
