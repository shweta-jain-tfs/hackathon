# SimpleTasks

A small Blazor Web App using .NET 10 and Interactive Server rendering.

## Run locally

Install the .NET 10 SDK, then run these commands from this directory:

```powershell
dotnet restore
dotnet build --no-restore
dotnet run --launch-profile http
```

Open the localhost address printed in the terminal. Stop with Ctrl+C.

## Included

- Add tasks with trimmed, required titles (maximum 120 characters).
- Mark tasks complete or active and delete tasks.
- Filter by All, Active, or Completed.
- See total, active, and completed counts.
- Responsive layout with labeled inputs and keyboard focus styles.
- Standard Blazor error handling and connection recovery UI.

## Project layout

```text
Components/Pages/Home.razor   Interactive task screen
Components/Layout/          Shared layout and connection recovery
Models/TaskItem.cs          Task data model
Services/TaskService.cs     Task operations and validation
wwwroot/app.css             Application styles
Program.cs                  Service registration and app configuration
```

TaskService is scoped to each Blazor circuit and starts with three sample tasks.
Changes stay in server memory and reset on a full page refresh or new session.
There is no database, authentication, or external service dependency.
To extend this starter, replace the in-memory service with persistent storage.

## Manual smoke check

1. Open the app: three sample tasks appear; one is complete.
2. Submit a blank title: validation appears and no task is added.
3. Add a task: it appears and the total and active counts increase.
4. Complete it: active count decreases and completed count increases.
5. Switch between filters and verify matching tasks appear.
6. Delete a task: it disappears and the counts update.
7. Refresh the page: the sample tasks return.
