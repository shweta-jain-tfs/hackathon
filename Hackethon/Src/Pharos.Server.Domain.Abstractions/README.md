# Development instructions for Pharos.Domain project

## Overview

This assembly is the "Domain" layer according to the ["clean architecture" template](https://github.com/jasontaylordev/CleanArchitecture).

## Adding new entities

Not every class should be in the "domain" assembly. Domain classes represent entities that must be persisted even if the system is shut down.

If you need to track all modifications of a new entity, just inherit it from `BaseAuditableEntity` class.
The changes will be automatically recorded in the audit log.

If the new entity has properties that must be encrypted at rest (in the database), 
mark those properties with SensitiveDataAttribute. They will be automatically encrypted.

To configure database persistence for the new entity, add `IEntityTypeConfiguration<>` implementation
to the `Pharos.Server.Infrastructure.Database\Configuration` folder (see base entity configurations in the folder).

See also [Pharos.Server.Infrastructure.Database/README.md](../Pharos.Server.Infrastructure.Database/README.md)
 