# winget manifest templates

The Release workflow fills these in - `{{VERSION}}`, `{{RELEASE_DATE}}`, `{{X64_SHA256}}`,
`{{ARM64_SHA256}}` - and submits them to [microsoft/winget-pkgs](https://github.com/microsoft/winget-pkgs)
with `wingetcreate submit`. Edit the package metadata here; the next release carries it.
See [DEVGUIDE.md → Release](../DEVGUIDE.md#release).
