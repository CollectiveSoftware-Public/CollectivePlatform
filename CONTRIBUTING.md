<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
# Contributing to CollectivePlatform

Thanks for your interest in improving the shared libraries behind the Collective Software suite.

## Getting started

1. Install the [.NET 10 SDK](https://dotnet.microsoft.com/download).
2. Fork and clone, then: `dotnet test CollectivePlatform.slnx -c Release`.

## Layout

- `src/Collective.Platform.Abstractions`, `src/Collective.Platform` — **BCL-only**; no UI framework may be referenced from these two (apps without a desktop head still consume them).
- `src/Collective.Platform.Controls` — the one UI package (Avalonia). Shared widgets and the design system live here; keep pure logic UI-free so it stays unit-testable.
- `src/Collective.Platform.Secrets` — secret stores and the device-key module.
- `src/Collective.Platform.Testing` — test doubles; never referenced by product code.
- `src/Collective.Update` — the security-critical updater. Do not change its verify/download/apply logic without a matching security review; it is intentionally shared verbatim across the suite so there is one audited copy.
- `tests/` — xunit suites per package.

## Conventions

- **License header:** every source file starts with `SPDX-License-Identifier: GPL-3.0-or-later` (line comment for `.cs`, XML comment for `.axaml`/`.csproj`).
- **The core stays UI-agnostic.** New shared abstractions or widgets are added here once and consumed, rather than re-declared in a product.
- **Tests come with changes.** Pure models are unit-tested; window construction has headless tests.
- **One commit per logical change**, with a clear message.

## Pull requests

Keep each PR focused; ensure `dotnet test CollectivePlatform.slnx` passes before opening; describe what changed and why. Please report security vulnerabilities privately per [SECURITY.md](SECURITY.md), not via a public issue.
