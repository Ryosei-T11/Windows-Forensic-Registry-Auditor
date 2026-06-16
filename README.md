# ForensicAuditor

ForensicAuditor is a Windows-focused registry and process auditing tool intended for defensive, incident-response, and forensic use. It monitors registry changes and suspicious process activity, surfaces real-time alerts in a desktop UI, and provides export and manual mitigation actions for authorized investigators.

## Key features
- Real-time registry monitoring and alerting (`ForensicAuditor.Engine`).
- Heuristic scoring and contextual metadata in `RegistryEvent` models.
- Desktop dashboard with filtering, export (CSV/JSON), and manual mitigation (`ForensicAuditor.UI.Dashboard`).
- Built-in mitigation helpers: process quarantine (`ForensicAuditor.Infrastructure.Mitigation.ProcessQuarantine`) and registry rollback (`ForensicAuditor.Infrastructure.Mitigation.RegistryRollbackEngine`).
- Structured logging with Serilog and optional OpenTelemetry integration.

## Architecture overview
- `ForensicAuditor.Core`: domain models and interfaces (e.g., `RegistryEvent`, `IRegistryMonitor`).
- `ForensicAuditor.Engine`: monitoring/orchestration, event ingestion and heuristic evaluation.
- `ForensicAuditor.Infrastructure`: platform helpers, mitigation engines, logging and telemetry.
- `ForensicAuditor.UI.Dashboard`: WPF UI for real-time viewing, filtering, and mitigation.

## Requirements
- .NET 10 (target framework in project files).
- Visual Studio 2022/2026 or `dotnet` SDK compatible with .NET 10.
- Windows (registry monitoring and process control use Windows APIs).

Notable NuGet dependencies (example from `ForensicAuditor.Infrastructure`):
- `Serilog`, `Serilog.Sinks.File`, `Serilog.Sinks.Console`
- `Microsoft.Diagnostics.Tracing.TraceEvent`
- `OpenTelemetry` packages

## Build and run
1. Clone the repository:
   - `git clone https://github.com/Ryosei-T11/Windows-Forensic-Registry-Auditor`
2. Open `ForensicAuditor.slnx` in Visual Studio (or use `dotnet build`).
3. Restore NuGet packages and build the solution.
4. Run `ForensicAuditor.UI.Dashboard` project to start the desktop application.
5. Use Visual Studio Test Explorer or `dotnet test` to run tests (if any).

## Configuration
- Logging and telemetry configuration are in the `Infrastructure` projects (Serilog / OpenTelemetry).
- UI filters, export, and mitigation controls are available in the dashboard (`RealTimeLogView` and `MainWindow`).

## Usage and safety
- This tool can perform destructive actions (process termination, registry rollback). Always:
  - Only operate on systems where you have explicit authorization.
  - Test in an isolated lab before using on production.
  - Maintain secure audit trails and tamper-resistant logs.
  - Limit who can perform mitigation actions (implement RBAC in production deployments).
- The repository is intended for defensive and investigative use only. No offensive guidance or instructions for misuse are provided.

## Files to review for mitigation behavior
- `ForensicAuditor.Infrastructure\Mitigation\ProcessQuarantine.cs`
- `ForensicAuditor.Infrastructure\Mitigation\RegistryRollbackEngine.cs`
- UI wiring: `ForensicAuditor.UI.Dashboard\Views\RealTimeLogView.xaml.cs`

Review these carefully before enabling mitigation in production.

## Contributing
Contributions welcome. Please:
- Open issues for bugs or improvement proposals.
- Submit PRs with descriptive titles and tests where applicable.
- Follow repository coding conventions and sign commits where required.

## License
Check the repository root for a `LICENSE` file. If none exists, contact the maintainers before using this in production.

## Contact / Support
For questions about the codebase, open issues on the repository or contact the project maintainers via the repository remote.
