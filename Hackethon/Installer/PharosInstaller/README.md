Use a markdown viewer for convenient reading (e.g. Notepad++ with MarkdownPanel plugin, VSCode, or VS).

# Pharos Installer Usage

This document describes installing Pharos using either of two provided installers:
1. PharosInstaller.exe (bootstrapper – recommended)
2. PharosInstaller.msi (direct msiexec usage)

---

## 1. Prerequisites

- Install SQL Server 2022 or later (Developer edition is sufficient for development / test) 
	- Default instance (MSSQLSERVER)
	- Windows Authentication mode
	- Ensure service “SQL Server (MSSQLSERVER)” is running

Run installer as an Administrator, needs to be same admin user that installs SQL Server as clean install needs to create SQL logins (sysadmin role needed).

---

## 2. Build / Obtain Installer

Local build:
- Place a `ParameterUpdatePackage-XXXXX.zip` file in the `ParameterUpdatePackage\` folder at the repository root. The folder is not version-controlled — create it manually if it does not exist.
- Build PharosInstaller solution
- PharosInstaller build output folder contains both .msi and .exe

Pipeline:
- The pipeline automatically fetches the `ParameterUpdatePackage-XXXXX.zip` from Azure DevOps Secure Files (Pipelines/Library/Secure Files) using the `parameterUpdatePackageVersion` parameter.
- Run main pipeline (automatically on merge to main or on demand)
- Download PharosInstaller folder from artifacts (contains .msi + .exe)

Deliver both files together.

---

## 3. Choosing .exe vs .msi

| Use | When |
|-----|------|
| PharosInstaller.exe | Normal installs, consistent log naming, simple parameters |
| PharosInstaller.msi | Advanced automation needing full msiexec control |

---

## 4. Bootstrapper (.exe)

Syntax:

````````
PharosInstaller.exe [options]
````````

### Parameters
| Name | Type | Default | Description |
|------|------|---------|-------------|
| secureenclavesenabled | bool | true | Enables/disables SQL Server's Always Encrypted with Secure Enclaves usage. Supported only on VMs run by Hyper-V. Disable on VMs run by other hypervisors such as VMWare. |
| quiet | bool | false | false => /passive (progress UI). true => /quiet (no UI). |
| testrollback | bool | false | Forces a deliberate failure near the end to verify rollback logic. Non-production only. |

Notes:
- Tokens must be in the form key=value (no spaces around '=').
- Parameters are optional; omitted parameters use defaults.
- Order does not matter.

### Examples
Examples:
````````
# Run installer with default settings
.\PharosInstaller.exe

# Disable secure enclaves
.\PharosInstaller.exe secureenclavesenabled=false

# Quiet (no UI)
.\PharosInstaller.exe quiet=true

# Full silent with multiple overrides
.\PharosInstaller.exe secureenclavesenabled=false quiet=true

# Test rollback (intentional failure)
.\PharosInstaller.exe testrollback=true

# Show help
.\PharosInstaller.exe help

````````

Behavior:
- Verifies Administrator rights
- Locates first .msi in same directory
- Launches: 
````````
msiexec /i "<msi>" (/passive | /quiet) /l*vx "<log>" secureenclavesenabled=<value> [testrollback=...]
````````

---

## 5. Direct MSI Usage

General form:
````````
PharosInstaller.msi [options]
````````

### Public Properties
| Property | Values | Default | Description | Mirrors EXE Parameter |
|----------|--------|---------|-------------|-----------------------|
| SECUREENCLAVESENABLED | true / false | true | Enable or disable Always Encrypted with Secure Enclaves usage. | secureenclavesenabled |
| TESTROLLBACK | true / false | false | Force rollback test (intentional failure). | testrollback |

Notes:
- Caller of the installer is responsible for setting log level and log path as per requirement (MR_3211)
- Property names are case-insensitive.
- If not supplied, defaults above apply.
- Use `/passive` for minimal UI or `/quiet` for none.
- Always supply a verbose log for diagnostics.
- Log path is optional; if omitted, msiexec logs to %TEMP% by default.

### Examples

Examples:
````````
# Quiet install with verbose logging to specified path (path needs to exist)
.\PharosInstaller.msi /quiet /l*vx "C:\ProgramData\Thermo Fisher Scientific\Pharos\InstallationLogs\install.log"

# Disable secure enclaves + silent install + default log path with verbose logging
.\PharosInstaller.msi SECUREENCLAVESENABLED=false /quiet /l*vx

# Rollback test (expected failure)
.\PharosInstaller.msi TESTROLLBACK=true /passive /l*vx C:\Temp\rollback.log

# Install with passive UI
.\PharosInstaller.msi /passive

# Install silently
.\PharosInstaller.msi /quiet

````````

- Log file path pattern:
````````
C:\ProgramData\Thermo Fisher Scientific\Pharos\InstallationLogs\Pharos-install-YYYYMMDD_HHmmss.log
````````

---

## 6. Modes (UI)

| Mode | Trigger | Notes |
|------|---------|-------|
| Passive | EXE default or /passive | Progress UI, no prompts |
| Quiet | quiet=true or /quiet | No UI or progress indicator |

---

## 7. Rollback Test

Purpose: Validate MSI custom actions revert state.

Exe:
````````
.\PharosInstaller.exe testrollback=true
````````

MSI:
````````
.\PharosInstaller.msi TESTROLLBACK=true /passive /l*vx C:\Temp\rollback.log
````````

Do not use in production.

---

## 8. Logs

| Type | Location |
|------|----------|
| Installer logs | C:\ProgramData\Thermo Fisher Scientific\Pharos\InstallationLogs |
| Application logs | C:\ProgramData\Thermo Fisher Scientific\Pharos\Logs |

On failure the bootstrapper references:
- Installer log path used
- Latest application log (if any)

---

## 9. Uninstallation

Via Add or remove programs

---

## 10. Reference

See: Pharos Feature Description – Installer (ID 811156) for deeper functional details.

---