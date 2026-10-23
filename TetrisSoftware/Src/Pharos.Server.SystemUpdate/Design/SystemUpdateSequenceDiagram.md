# System Update Sequence Diagram

This document describes the system update flow from USB insertion through update completion and acknowledgment.

## Overview

The system update process involves:
- **User**: Operator initiating the update
- **Client**: Blazor WebAssembly application running in the browser
- **Server**: Pharos.Server backend application
- **Installer**: Windows MSI installer process

## USB Update Package Prerequisites

- The USB root must contain an `Updates` folder, for example `E:\Updates`.
- The `Updates` folder must contain exactly one `.zip` package with the Pharos `.msi` installer inside.
  - Valid package path examples: 
    - `E:\Updates\Pharos_0.1.0.20007.zip` with `Pharos_0.1.0.20007.msi` inside.
    - `E:\Updates\PharosInstaller.zip\PharosInstaller` with `Pharos_0.1.0.20007.msi` inside. (Recursive extraction)

## Sequence Diagram

```mermaid
sequenceDiagram
    autonumber

    participant User
    participant Client
    participant Server
    participant Installer

    %% Phase 1: USB Detection and Validation
    Note over User, Server: 1️⃣ USB Detection & Validation
    User->>Server: Insert USB drive
    Server->>Server: Validate USB
    alt Invalid USB
        Server-->>Server: Ignore drive
    else Valid USB
        Server->>Server: Register update medium
        Server->>Server: Enumerate available updates
        Server->>Server: Create TaskCard (UpdatesAvailable)
        Server-->>Client: UpdatesAvailable notification (SignalR)
        Client->>Client: Display task card indicator
    end

    %% Phase 2: User initiates update
    Note over User, Client: 2️⃣ User Initiates Update
    User->>Client: Navigate to About page
    Client->>Server: GET /api/systemupdate/availableUpdates
    Server-->>Client: List of updates
    Client->>Client: Display updates available
    User->>Client: Press "Update Now"
    Client->>Server: POST /api/systemupdate/initiateUpdate

    %% Phase 3: Server-side update process
    Note over Server, Installer: 3️⃣ Update Execution
    Server->>Server: Record pending update session (store previous version)
    Server->>Server: Stage update package to temp directory
    Server-->>Client: UpdateInProgress notification (SignalR)
    Client->>Client: Show "Update in Progress" modal
    Server->>Installer: Start MSI installer process
    Installer->>Installer: Uninstall Pharos Server
    Client->>Client: Connection lost (server unavailable)
    Installer->>Installer: Install new Pharos Server version
    Installer-->>Server: Installation complete (server restarts)

    %% Phase 4: Post-update reconnection
    Note over Client, Server: 4️⃣ Reconnection & Outcome Evaluation
    Server->>Server: Application started
    Server->>Server: Evaluate update outcome (compare versions)
    alt Version increased
        Server->>Server: Mark update as Successful
    else Version unchanged
        Server->>Server: Mark update as Failed
    end
    Client->>Client: Detect connection restored
    Client->>Server: GET /api/systemupdate/unacknowledgedOutcome
    Server-->>Client: Unacknowledged outcome (success/fail)

    %% Phase 5: Update outcome display and acknowledgment
    Note over User, Server: 5️⃣ Outcome Display & Acknowledgment
    Client->>Client: Close "Update in Progress" modal
    Client->>Client: Show update outcome modal (success/fail)
    User->>Client: Close outcome modal (any client)
    Client->>Server: POST /api/systemupdate/acknowledgeUpdate
    Server->>Server: Record acknowledgment timestamp
    Server-->>Client: UpdateOutcomeAcknowledged broadcast (SignalR)
    Client->>Client: All clients close outcome modal
    alt Update was successful
        Client->>Client: Reload application
    end
```

## Update Session States

```mermaid
stateDiagram-v2
    [*] --> Pending: Update initiated
    Pending --> Successful: Version increased after restart
    Pending --> Failed: Version unchanged after restart
    Successful --> [*]: Acknowledged
    Failed --> [*]: Acknowledged
```
