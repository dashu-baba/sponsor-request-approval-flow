# React Best Practice Rules

A practical rulebook for building maintainable, production-ready React applications.

## Core Rules

### 1. Keep components focused
A component should have one clear UI responsibility. If a component handles layout, data fetching, form logic, table rendering, modal state, and API calls together, split it.

### 2. Prefer composition over inheritance
Build UI by composing small components. Pass children, slots, render props, or component props instead of creating deep inheritance-like abstractions.

### 3. Keep state as local as possible
Put state near the component that actually needs it. Do not move state to global stores just because it is convenient.

### 4. Avoid duplicated or contradictory state
Do not store values that can be derived from existing state or props. Derived state creates synchronization bugs.

Bad:

```tsx
const [firstName, setFirstName] = useState('John');
const [lastName, setLastName] = useState('Doe');
const [fullName, setFullName] = useState('John Doe');
```

Better:

```tsx
const fullName = `${firstName} ${lastName}`;
```

### 5. Do not overuse `useEffect`
Use effects to synchronize React with external systems: network, browser APIs, subscriptions, timers, DOM libraries, or analytics. Do not use effects for normal render calculations.

### 6. Put event logic in event handlers
If something happens because the user clicked, submitted, selected, or typed, keep that logic in the event handler instead of moving it into an effect.

### 7. Always clean up effects
If an effect creates a subscription, timer, event listener, or fetch operation, it should have cleanup logic.

```tsx
useEffect(() => {
  const controller = new AbortController();

  fetch('/api/users', { signal: controller.signal });

  return () => controller.abort();
}, []);
```

### 8. Use stable keys for lists
Never use array index as a key when list items can be inserted, removed, sorted, or filtered. Use a stable ID from the data.

### 9. Treat props and state as immutable
Do not mutate arrays or objects directly. Create a new array/object when updating state.

Bad:

```tsx
items.push(newItem);
setItems(items);
```

Better:

```tsx
setItems((current) => [...current, newItem]);
```

### 10. Keep server state separate from UI state
Data from APIs is server state. Loading flags, selected tab, modal open/close, and form drafts are UI state. For complex server state, use tools like TanStack Query, RTK Query, SWR, or framework-native data loading.

### 11. Validate external data
TypeScript types do not validate runtime API responses. For important APIs, validate external data using schema validators such as Zod, Valibot, or backend-generated contracts.

### 12. Do not memoize everything
Use `memo`, `useMemo`, and `useCallback` when profiling or real behavior shows a benefit. Manual memoization adds complexity and can be useless when props still change every render.

### 13. Split large bundles
Lazy-load routes, heavy pages, charts, editors, maps, and rarely used flows. Do not lazy-load tiny components just for the sake of it.

### 14. Use forms intentionally
For simple forms, controlled inputs are fine. For large forms, prefer React Hook Form or similar libraries to reduce unnecessary re-renders and simplify validation.

### 15. Keep API calls out of random components
Centralize API access behind typed client functions, services, or hooks. Components should not know every low-level endpoint detail.

### 16. Handle loading, empty, error, and success states
Every API-driven UI should explicitly handle these four states. Do not design only the happy path.

### 17. Avoid prop drilling through many layers
If props pass through many components that do not use them, consider composition, context, or a scoped store.

### 18. Use Context carefully
Context is good for app-wide or section-wide values like auth user, theme, locale, or feature flags. Avoid putting frequently changing large state in one global context because it can cause broad re-renders.

### 19. Write accessible UI by default
Use semantic HTML first. Buttons should be buttons, links should be links, inputs should have labels, dialogs should manage focus, and keyboard navigation should work.

### 20. Test behavior, not implementation details
Test what the user sees and does. Prefer React Testing Library for component behavior and Playwright/Cypress for end-to-end flows.

### 21. Keep styling consistent
Choose one styling approach per project: CSS modules, Tailwind, styled-components, vanilla-extract, or design-system components. Avoid mixing too many patterns.

### 22. Use TypeScript strictly
Enable strict mode. Type props, API responses, form values, and event handlers. Avoid `any`; use `unknown` for external data and validate it.

### 23. Use error boundaries
Use error boundaries around major UI sections so one broken component does not crash the entire app.

### 24. Keep rendering pure
Do not perform side effects during render. Rendering should calculate UI from props and state.

### 25. Measure before optimizing
Use React DevTools Profiler, browser performance tools, and bundle analyzers before adding performance complexity.

## Recommended Project Structure

```txt
src/
  app/
  components/
  features/
    users/
      components/
      hooks/
      api/
      types.ts
  hooks/
  lib/
  routes/
  styles/
  tests/
```

## Senior Rule

Keep state minimal, effects rare, components focused, data access typed, and performance work evidence-based.
