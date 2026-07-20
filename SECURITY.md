<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
# Security Policy

## Reporting a vulnerability

Please report security vulnerabilities **privately** — do not open a public issue.

Use GitHub's private vulnerability reporting: on this repository, open the **Security** tab and choose **Report a vulnerability**. This creates a private advisory visible only to the maintainers. We aim to acknowledge reports within a few days. Please allow a reasonable window for a fix to ship before any public disclosure.

## Scope

CollectivePlatform is a set of shared libraries for local-first desktop applications. Of particular interest:

- **`Collective.Update`** — the auto-update chain: release-manifest signature verification (ECDSA P-256/SHA-256 against pinned public keys), artifact hash checking, and the atomic swap-and-restart. The verify core is designed to fail closed: tampered, wrong-key, truncated, or garbage input must be rejected, and the app must fetch and execute only the artifact the signed manifest declares for its exact platform. A compromised release-signing key that produces a genuinely-signed malicious release is an accepted residual risk (the key is held offline, never in CI).
- **`Collective.Platform.Secrets`** — the device-key cipher, the sealed credential store, and the OS secret backends.
- **`Collective.Platform`** — shared file and settings IO consumed across the suite.

## Supported versions

The suite is in active development; security fixes target the latest package versions and `main`.
