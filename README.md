# Umbraco YAML Toolkit — Umbraco 13

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![CI](https://github.com/SplatDev-Ltda/umbraco-yaml/actions/workflows/ci.yml/badge.svg)](https://github.com/SplatDev-Ltda/umbraco-yaml/actions/workflows/ci.yml)

> **Branch `support/umbraco-13`** — Umbraco 13 (net8.0) only.
> For Umbraco 14-17 support, see the `master` branch.

An **Infrastructure-as-Code** plugin that exports your entire Umbraco 13 site structure as a human-readable YAML file.

```
┌──────────────────────────────────────────────────────────────────┐
│  Umbraco 13 site  ──Schema2Yaml──▶  umbraco.yml + media/        │
│                                          │                       │
│                                     Git / CI                     │
│                                          │                       │
│  Target site      ◀─Yaml2Schema───  umbraco.yml + media/        │
└──────────────────────────────────────────────────────────────────┘
```

---

## Package

### [SplatDev.Umbraco.Plugins.Schema2Yaml](SplatDev.Umbraco.Plugins.Schema2Yaml/README.md)

[![NuGet](https://img.shields.io/nuget/v/SplatDev.Umbraco.Plugins.Schema2Yaml.svg)](https://www.nuget.org/packages/SplatDev.Umbraco.Plugins.Schema2Yaml)
[![NuGet Downloads](https://img.shields.io/nuget/dt/SplatDev.Umbraco.Plugins.Schema2Yaml.svg)](https://www.nuget.org/packages/SplatDev.Umbraco.Plugins.Schema2Yaml)

Exports your existing Umbraco site structure to YAML. Adds a **Schema Export** dashboard in the Settings section. One click produces a ZIP with a complete `umbraco.yml` and all media files.

```bash
dotnet add package SplatDev.Umbraco.Plugins.Schema2Yaml --version "1.0.*"
```

**Compatibility:** Umbraco 13.x / net8.0

---

## Repository Structure

```
umbraco-yaml/ (support/umbraco-13)
├── SplatDev.Umbraco.Plugins.Schema2Yaml/          # Exporter package (Umbraco 13)
├── SplatDev.Umbraco.Plugins.Schema2Yaml.Tests/    # Unit & integration tests
├── Umbraco.Web/                                   # Test site (Umbraco 13 + Starter Kit)
├── docs/                                          # Authoring guide and images
└── .github/workflows/                             # CI + NuGet publish workflows
```

---

## Quick Start

1. Install the NuGet package in your Umbraco 13 project
2. Start the site — the dashboard appears in **Settings > Schema Export**
3. Click **Export to YAML** to generate the export
4. Click **Download ZIP** to get `umbraco.yml` + all media files

---

## Contributing

Issues and pull requests welcome at <https://github.com/SplatDev-Ltda/umbraco-yaml>.

## License

MIT © 2026 SplatDev
