using Xunit.Sdk;
using Xunit.v3;
using xUnitV3LoadFramework.Extensions.ObjectModel;

namespace xUnitV3LoadFramework.Extensions.Runners;

public class LoadTestRunner :
    TestRunner<ObservationTestRunnerContext, LoadTest>
{
    public static LoadTestRunner Instance { get; } = new();

    // We don't want to claim to create or dispose the object here, because we share an already created
    // instance among all the tests, and we don't want to dispatch the messages related to creation
    // and disposal either. So we return false for the creation/disposal options.
    protected override ValueTask<(object? Instance, SynchronizationContext? SyncContext, ExecutionContext? ExecutionContext)> CreateTestClassInstance(ObservationTestRunnerContext ctxt) =>
        throw new NotSupportedException();

    protected override bool IsTestClassCreatable(ObservationTestRunnerContext ctxt) =>
        false;

    protected override bool IsTestClassDisposable(
        ObservationTestRunnerContext ctxt,
        object testClassInstance) =>
        false;

    protected override ValueTask<TimeSpan> InvokeTest(
        ObservationTestRunnerContext ctxt,
        object? testClassInstance) =>
        base.InvokeTest(ctxt, ctxt.Specification);

    public async ValueTask<RunSummary> Run(
        Specification specification,
        LoadTest test,
        IMessageBus messageBus,
        string? skipReason,
        ExceptionAggregator aggregator,
        CancellationTokenSource cancellationTokenSource)
    {
        await using var ctxt = new ObservationTestRunnerContext(specification, test, messageBus, skipReason, aggregator, cancellationTokenSource);
        await ctxt.InitializeAsync();

        return await Run(ctxt);
    }
}

public class ObservationTestRunnerContext(
    Specification specification,
    LoadTest test,
    IMessageBus messageBus,
    string? skipReason,
    ExceptionAggregator aggregator,
    CancellationTokenSource cancellationTokenSource) :
    TestRunnerContext<LoadTest>(test, messageBus, skipReason, ExplicitOption.Off, aggregator, cancellationTokenSource, test.TestCase.TestMethod.Method, [])
{
    public Specification Specification { get; } = specification;
}