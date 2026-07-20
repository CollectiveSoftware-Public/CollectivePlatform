<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
# CollectivePlatform

**Shared platform libraries for the [Collective Software](https://github.com/CollectiveSoftware-Public) suite** — the one place the common abstractions, shared desktop widgets, the suite design system, the device-key secret stores, and the in-app auto-updater live, so the product apps consume them instead of copy-porting.

Free software under the **GNU GPL v3 or later**. This repository is the corresponding source for the `Collective.*` packages bundled into the suite's published applications (e.g. CollectiveGit).

## Packages

- **`Collective.Platform.Abstractions`** — BCL-only interfaces (`IFileSystem`, `ISettingsStore`, directory-listing seams). No UI framework.
- **`Collective.Platform`** — BCL-only implementations: settings store, per-product app-data paths, the file-tree/file-operations module, and small shared services (recent-items, crash guard, idle lock).
- **`Collective.Platform.Controls`** — shared Avalonia widgets and the suite design system (theme-aware tokens, the Refined Fluent style pack, the file explorer, dialogs, the document-tab strip, the window module).
- **`Collective.Platform.Secrets`** — the `ISecretStore` seam and concretes (DPAPI, encrypted-file, and the device-key module) plus a sealed credential store.
- **`Collective.Platform.Testing`** — BCL-only deterministic test doubles for the abstractions.
- **`Collective.Update`** — the suite's shared in-app auto-updater: a signed release manifest (ECDSA P-256) is verified fail-closed, the declared artifact is hash-verified, then applied by an atomic file swap with rollback. Consent-first; failures never break the installed app.

## Build & consume

Targets **.NET 10**.

    git clone https://github.com/CollectiveSoftware-Public/CollectivePlatform.git
    cd CollectivePlatform
    dotnet test CollectivePlatform.slnx -c Release

Consistent with the suite's no-server stance, the packages are distributed through a **committed offline folder feed**, not a NuGet server. Build it with `build/pack.ps1` (it writes `feed/*.nupkg`); a consumer references the feed via `nuget.config` and vendors the `.nupkg` it needs (a committed copy) so restore works offline. The generated `feed/` is not checked in here — it is a build artifact you regenerate from source.

## License

**GNU General Public License, version 3 or later** (`GPL-3.0-or-later`). See [LICENSE](LICENSE). Every source file carries an SPDX header. Please report security vulnerabilities privately per [SECURITY.md](SECURITY.md).
