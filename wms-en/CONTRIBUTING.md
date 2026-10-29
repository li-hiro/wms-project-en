# Contributing Guide

Thanks for your interest in this project! Please read the following guidelines before contributing.

## Filing Issues

- Search first to check whether an identical or similar issue already exists.
- Bug reports should include: reproduction steps, expected result, actual result, and environment info (.NET / Node versions, operating system).
- Feature requests should describe the use case and expected benefit, not just the implementation.

## Development Workflow

1. Fork the repository and clone it locally.
2. Create a branch from `main`:
   - New features: `feat/feature-name`
   - Bug fixes: `fix/bug-name`
   - Documentation: `docs/topic`
3. Develop and verify locally (see [README](README.md#local-setup)).
4. Open a Pull Request describing the motivation, key implementation points, and how you verified the change.

## Code Style

### Backend (C# / .NET 8)

- Keep the module dependency direction: `WebApi → App → Im → Core`, `Plugin → Core`. Reverse references and circular dependencies are forbidden.
- Shared capabilities (entity base classes, utilities, exceptions, permission codes) go in `Wms.Core`; business orchestration goes in `Wms.App`; inventory rules go in `Wms.Im`; extensions go in `Wms.Plugin`.
- Keep external DTOs separate from entities. Request models use data annotations for basic validation; business validation throws `BusinessException` / `NotFoundException` / `ConflictException`.
- Controller methods must return `ApiResult<T>` and explicitly declare `[HasPermission]`; use `xxx:view` for queries, `xxx:edit` for writes, and `xxx:execute` for executions.
- Add XML comments to all public members; use PascalCase / camelCase naming; async methods end with `Async`.
- Database schema changes must update the `WmsDbContext` configuration and seed data in sync.

### Frontend (Vue 2)

- Pages live under `src/views` grouped by module; API wrappers live under `src/api`.
- Vuex only holds auth state, permissions, and menus; page-level data stays inside components.
- All requests go through `src/api/request.js`; never use axios directly inside components.
- Buttons and routes must both bind permission codes; new pages must update both the backend menu seed data and the frontend static routes.
- Style: 2-space indentation, single quotes, semicolons optional but consistent within a file.

## Commit Messages

Use Conventional Commits:

```
feat(im): add stock lock and release capability
fix(app): write back shipped quantity on outbound completion
docs: add plugin development guide
chore: bump dependency versions
```

## Pre-Submission Checklist

- [ ] `dotnet build backend/Wms.sln` succeeds with no new warnings
- [ ] `npm run build` (frontend) succeeds
- [ ] New endpoints verified in Swagger with working permission attributes
- [ ] New pages tested in the browser with no console errors
- [ ] Docs and comments updated in sync
