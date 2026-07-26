# Sanitized iPhone release receipt

Verified: 2026-07-26
Source revision: `29a13163d97b5f0348b0781c3b87150f8a329ac8`

- Unity `6000.3.19f1` exported the iOS Xcode project with
  `BuildOptions.None`.
- Xcode `26.6` built the `Release-iphoneos` configuration successfully.
- The application executable contains only `arm64`.
- Application bundle identifier: `dev.kikuai.wobblestack`.
- Embedded Unity framework bundle identifier: `com.unity3d.framework`.
- No `Development Build` or `DEVELOPMENT_PLAYER` marker was found.
- `codesign --verify --deep --strict` passed.
- CoreDevice replacement install passed.
- CoreDevice installed-app readback returned `Wobble Stack` version `1.0`.
- CoreDevice launch passed and live-process readback returned the same launched
  process.

Signing identities, provisioning details, device identifiers, installation
paths, and process identifiers are intentionally excluded. Physical control,
pacing, performance, and voluntary-Retry acceptance remain the owner's device
gate.
