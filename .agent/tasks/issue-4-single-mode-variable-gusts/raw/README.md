# Sanitized verification receipts

Raw signing identities, provisioning data, local paths outside the repository,
and physical-device identifiers are excluded from this public repository.

Sanitized local receipts cover:

- web unit tests and production build;
- Unity EditMode and PlayMode XML reports;
- a fresh Mac smoke build and inspected `1179 × 2556` start-screen capture;
- a non-Development Unity iOS export;
- Xcode `BUILD SUCCEEDED`, strict signature verification, and arm64 executable;
- successful replacement install and launch on the paired iPhone.

The first install attempt lost its device transport connection. The same
already-built and signature-verified app installed and launched successfully
on the next attempt without rebuilding or changing source.
