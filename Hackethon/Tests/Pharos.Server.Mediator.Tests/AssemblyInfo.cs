using Xunit.Sdk;
using Xunit.v3;

// ServiceRegistrar stores the generic handler registration limits (MaxTypesClosing, RegistrationTimeout, etc.)
// in mutable static fields that every AddMediatR call overwrites. Tests running in parallel can overwrite each
// other's limits in the middle of registration, which causes intermittent failures
// (e.g. ShouldThrowExceptionWhenTimeoutOccurs throwing ArgumentException instead of TimeoutException).
[assembly: Parallelization(Mode = ParallelMode.None)]
