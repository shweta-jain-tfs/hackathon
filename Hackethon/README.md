Pharos Software
===============

Pharos is a user control software for the new generation of Phadia instruments developed within the Tetris program.
This document contains instructions for developers.

__Table of contents:__

* [How to compile and run Pharos in debug mode](#how-to-compile-and-run-pharos-in-debug-mode)
* [General development guidelines](#general-development-guidelines)
* [Database development](#database-development)
* [Frontend development](#frontend-development)
* [Developing and running xUnit tests](#developing-and-running-xunit-tests)
* [Manual testing](#manual-testing)

How to compile and run Pharos in debug mode
-------------------------------------------

### 1. Install Microsoft Visual Studio and SQL Server

First, you need to install Microsoft Visual Studio and SQL Server if they are missing on your computer

* [Microsoft Visual Studio Professional 2022](https://visualstudio.microsoft.com/downloads/)
* [Microsoft SQL Server Developer edition](https://www.microsoft.com/en-gb/sql-server/sql-server-downloads) (version 2019 or higher)


### 2. Install Node.js

Node.js is used as the frontend development tool. Without it you cannot compile CSS properly.

We recommended you to use [Node.js version manager for Windows](https://github.com/coreybutler/nvm-windows)  for installing Node.js and npm.
1. Uninstall any pre-existing Node.js installations.
2. [Download](https://github.com/coreybutler/nvm-windows/releases) the latest nvm-windows installer.
3. Install nvm-windows.
   * If NVM4W doesn't appear to work immediately after installation, restart the terminal/powershell (not the whole computer).
4. Open Windows Terminal (Powershell) and run the following commands (In some cases you need to run terminal/powershell in the Administrator mode).

```
nvm install 20.11.0
nvm use 20.11.0
npm install -g nswag@14.7.1
```
*Note:* The nswag version should be the same as the version for the `NSwag.AspNetCore` nuget package in `Pharos.Server.csproj`.

To check that you are running correct versions, run the following command:
```
node -v
```

### 3. Update Powershell and git hooks

Make sure that you have the latest version of Powershell installed by running the following command. The Git hooks script requires version 7+.
```
winget install --id Microsoft.PowerShell --source winget
```

To minimize merge conflicts in .resx files, run the command: 
```
git config core.hooksPath .githooks
```
This will set up a git hook that runs a C# software that sorts the XML elements in .resx files that are changed before each commit has completed. This minimizes the risk of merge conlicts.

### 4. Install WiX tools

You can skip this step is you are not going to compile Pharos installer package.

* [HeatWave for VS2022](https://marketplace.visualstudio.com/items?itemName=FireGiant.FireGiantHeatWaveDev17) - A set of tools for creating WiX installation packages.


### 5. Add the default and Audit Trail connection string to the config file

1. Open `Pharos.sln` in Visual Studio
2. Create `Src\Pharos.Server\appsettings.Development.json` file
3. Add the following code to `appsettings.Development.json`.
```
{
  "ConnectionStrings": {
    "Default": "Server=localhost;Database=Pharos;TrustServerCertificate=True;Trusted_Connection=True;MultipleActiveResultSets=True;Column Encryption Setting=Enabled;Attestation Protocol=None",
    "Audit": "Server=localhost;Database=AuditTrail;TrustServerCertificate=True;Trusted_Connection=True;MultipleActiveResultSets=True;"
  }
}
```
The `Server` property may vary if you use non-default installation of the development SQL Server.

### 6. Build Pharos solution and run it

Assuming that the Pharos solution is open in Visual Studio:

1. Set `Pharos.Server` as the startup project.
2. Build the solution in Visual Studio.
	 See the [Pharos.Common.Web/README.md](./Src/Pharos.Common.Web/README.md) for additional details on managing CSS.
3. Run the debugger
   * Accept the development TLS certificate when prompted by VS.

General development guidelines
------------------------------

The following Docan documents contain Pharos design descriptions, architecture overview, and development guidelines:
* DocID [824899](https://idd-docanportal.emea.thermo.com/docan/diaurl?DocNo=824899) - high-level design description of Pharos with links to other documents.
* DocID [824905](https://idd-docanportal.emea.thermo.com/docan/diaurl?DocNo=824905) - server-side design description and guidelines.
* DocID [825133](https://idd-docanportal.emea.thermo.com/docan/diaurl?DocNo=825133) - client-side design description and guidelines.

_Note: If a document is still in the draft it may not be accessible via the link above. In this case go to the old [Docan](http://dctm/docan/component/main) and search by the DocId._

Following are the links to readme files with specific development instruction on different Pharos subsystems.
* [How to work with settings](Src/Pharos.Server.Settings/README.md)
* [How to work with task cards](Src/Pharos.Server.TaskCard/README.md)

Database development
--------------------

The project uses code-first approach to database development. This means, that database entities need to be defined
in the "domain" layer first (as POCO classes) and then configured in the infrastructure layer.

You need to add a database migration every time you change domain entities in the domain.

For more information, refer to the [P2137 Pharos Server Software Design Description, 824905](https://idd-docanportal.emea.thermo.com/docan/diaurl?DocNo=824905)
document in Docan (sections about domain entities and database design).

### EF Core tools

First, you'll have to install the [EF Core command-line tools](https://learn.microsoft.com/en-us/ef/core/cli/dotnet):

* It's recommended to use the .NET Core CLI tools, which work on all platforms.
* If you're more comfortable working inside Visual Studio or have experience with EF6 migrations, you can also use the [Package Manager Console tools](https://learn.microsoft.com/en-us/ef/core/cli/powershell).

### Database baseline

While Pharos is not under change control and the database schema is not well defined
use the following procedure to re-create the baseline database schema for a module.

1. Delete Migrations folder in the module project
2. Open Package Manager Console window in Visual Studio
3. In the following command, replace `<ModuleName>` with the name of the module and run it.

| .NET CLI |
| :------------ |
| `dotnet ef migrations add InitialCreate --project Pharos.Server.<ModuleName> --context <ModuleName>DbContext --startup-project Pharos.Server --output-dir Infrastructure/Database/Migrations` |

| Visual Studio |
| :------------ |
| `Add-Migration InitialCreate -Project Pharos.Server.<ModuleName> -Context <ModuleName>DbContext -StartupProject Pharos.Server -OutputDir Infrastructure/Database/Migrations` |


### Adding migrations with change control

To add a regular database migration use the following command template:

| .NET CLI |
| :------------ |
| `dotnet ef migrations add <MigrationName> --project Pharos.Server.<ModuleName> --context <ModuleName>DbContext --startup-project Pharos.Server --output-dir Infrastructure/Database/Migrations` |

| Visual Studio |
| :------------ |
| `Add-Migration <MigrationName> -Project Pharos.Server.<ModuleName> -Context <ModuleName>DbContext -StartupProject Pharos.Server -OutputDir Infrastructure/Database/Migrations` |


`<MigrationName>` - is a short CamelCaseDescription of the database schema changes.
`<ModuleName>` - is a name of the module that you need to change.

Next time Pharos is started these changes will be applied to the database.
You can apply them manually by executing `database update` parameter to the migration command above.

Frontend development
--------------------

### How to generate client-side code for APIs

You need to generate the client-side API access code every time you make any change in API endpoint or models they use.
If you are merging you local branch with a remote branch (e.g uplift from main) and both have made changes in the generated file.
Choose to take the incoming file and then regenerate the file with the `nswag_run.bat` batch file (see steps below).

1. Open Windows Terminal (Powershell) and change directory to `Src\Pharos.Client.Common.Models`.
2. If you have not installed nswag, CLI install the same version as the NSwag.AspNetCore reference in Pharos.Server.csproj for example: `npm install -g nswag@14.6.3` (see also section '2. Installation Instructions').
3. Run the `nswag_run.bat` batch file to generate the client api.

### Working with Blazor components

Refer to the following readme files for specific instructions:

* [Frontend components](Src/Pharos.Client/ReadMe.md)
* [Creating and updating components and styling](./Src/Pharos.Common.Web/README.md)

### How to override device type (UI layout) in development environment

The Pharos server will try to auto-detect type of the client device that has made an HTTP call.
Then, Pharos will render the appropriate client UI in the client device browser.

At the moment the following types of UI layouts are supported:
* Desktop (fallback mode)
* Instrument
* Tablet

By default, in a development environment the Pharos database is seeded with an instrument record.
This means that, __by default, Pharos will load the Instrument UI__.

If you need to override the automatic UI layout selection, add the following section to  `appsettings.Development.json`.

```
  "Client": {
    "ForcedClientDeviceType": "Desktop"
  },
```

For the list of supported values for `ForcedClientDeviceType` see the `ClientDeviceType` enum.

### Debug1080p development configuration

Use the `Debug1080p` solution configuration to run the instrument UI with a 1920x1080 viewport guide during development.

### Troubleshooting

Running in debug mode may sometimes cause the frontend to start with only a blank page.
This is usually followed by a crash and an error message saying that Visual Studio failed to launch a debug adapter.
To avoid this problem just delete the hidden folder named .vs (inside the "root" folder).

Developing and running xUnit tests
----------------------------------

### General guidelines

1. Your test project structure should reflect the the project under test (unless you are writing integration tests that cover several modules).
2. Change the default project namespace of the test project to match the project under test. You'll need fewer `using` directives.
3. Name unit-test classes using the pattern `ClassUnderTest` + `Tests`.

### Unit vs integration tests

It's quite rare when xUnit tests are limited to one class only. Thus, it's hard to draw the line between unit and integration tests.
In this project, we use the word "integration" for those test that use, at least, the real database (as opposed to DbContext test doubles).

You don't have to put all integration tests to one projects. If you want to add integration tests that are limited to one module,
put the test to this module's test project. See `Pharos.Server.Parameters.Tests` project for an example, specifically,
`ParametersTestsWaf.cs`, `ParametersTestsCollection.cs`, and `ImportArticlesCommandTests.cs`.

### How to run xUnit tests that use database

When you run xUnit tests they cannot use the database connection string from the `appsettings.Development.json` file.
You need to pass the connection string through an environment variable.

The environment variable name must be `ConnectionStrings__{ConnectionStringName}`. For the default connection string
it is `ConnectionStrings__Default`.

You can pass custom environment variables to Visual Studio using the `.runsettings` file. Create the `.runsettings`
file with the following content in the same folder as the `Pharos.sln` file.

```xml
<?xml version="1.0" encoding="utf-8"?>
<RunSettings>
  <RunConfiguration>
    <EnvironmentVariables>
      <DataProtection__EnableDataProtection><![CDATA[false]]></DataProtection__EnableDataProtection>
      <ConnectionStrings__Default>Database1 connection string</ConnectionStrings__Default>
      <ConnectionStrings__Parameters>Database2 connection string</ConnectionStrings__Parameters>
      <ConnectionStrings__Sample>Database2 connection string</ConnectionStrings__Sample>
    </EnvironmentVariables>
  </RunConfiguration>
</RunSettings>
```

The same environment variables must also be passed to the build pipeline that runs the tests.

__IMPORTANT NOTE:__

Every connection string above must point to a different database, because they are used in
the different xUnit test collections. And the tests in the different collections are not guaranteed to run
sequentially. This means that two tests in different collections that use the database, can cause random failures.

An xUnit test collection cannot be shared between different test projects.

### How to write data-driven xUnit tests

A data-driven test is a test that uses data from a file (as opposed to a test with hard-coded input values).

To create a test that reads values from a file:
1. Create data file and put it next to the xUnit test class.
2. Open properties window for the data file.
3. Set `Copy to Output Directory` property value to `Copy always`.
4. In the test method use the following line to get the full path to the data file.

```C#
var dataFilePath = (Path.Combine(DataDrivenTestHelper.GetTestDataFilesPath(GetType()), "MyData.csv");
```

See `ImportArticlesCommandTests.cs` for an example.

Manual testing
--------------

Sensitive information is redacted from the Pharos log file by default. To enable logging of sensitive information,
you need to set the `RedactSensitiveData` property to `false` in the `appsettings.Development.json` file.
And yet, you need the Debug build configuration to get unredacted logs.

### How to test Pharos with multiple instrument simulators

You can connect multiple instrument simulators to Pharos from the same host (most probably from localhost).

* Make sure that every simulator instance has a unique serial number for the instrument.
* The instrument UI works with the instrument that has connected first (plan the connect sequence).

### How to test Pharos API from the command line

To access Pharos API from the command line or tools like Postman, you need to generate an access token.

To get a test access token (valid for 24 hours) you need to execute the following HTTP request to the
`/login` endpoint on the Pharos server. The following example works in the Powershell console when debugging Pharos.

```
$LoginParameters = @{
    Uri         = 'https://localhost:7042/login'
    Method      = 'POST'
	ContentType = 'application/json'
	Body        = '{ "username": "your_user", "password": "your_password" }'
}
$Result = Invoke-WebRequest @LoginParameters
$Result.Content
```

If you run above code, you get the following JSON output. Copy access_token value and use id for
[bearer authentication](https://swagger.io/docs/specification/authentication/bearer-authentication/) in API requests.

```
{
  "token_type": "Bearer",
  "access_token": "eyMdAk5 ... really long string ... 9bw.isC7QblhOznHoCrBYy7zv90JEf739l1DYjRO0u5CwsQ",
  "expires_in": 3600",
  "refreshToken": "CfDJ8Dv-Ucz ... mtx7l"
}
```

When the access token expires, you can use the refresh token to get a new access token.

```
$LoginParameters = @{
    Uri         = 'https://localhost:7042/refresh'
    Method      = 'POST'
	ContentType = 'application/json'
	Body        = '{ "refreshToken": "CfDJ8Dv-Ucz5CVB ... gds34" }'
}
$Result = Invoke-WebRequest @LoginParameters
$Result.Content
```

If you build Pharos in the Debug configuration, you can also create a user account.

```
$RegisterParameters = @{
    Uri         = 'https://localhost:7042/register'
    Method      = 'POST'
	ContentType = 'application/json'
	Body        = '{ "username": "your_user", "password": "your_password" }'
}
Invoke-WebRequest @RegisterParameters
```
