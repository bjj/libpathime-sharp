using Xunit;

// libpathime does no locking and forbids overlapping calls — even across
// different engines and contexts. Never re-enable parallelization.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
[assembly: AssemblyFixture(typeof(PathimeSharp.Tests.PathimeFixture))]
