using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using LoadRunnerCore.Models;
using LoadRunnerCore.Runner;
using Xunit.Abstractions;
using Xunit.Sdk;
using xUnitLoadRunner;

// [assembly:Xunit.TestFramework("xUnitLoadFramework.CustomTestFramework", "CustomTestFramework")]

namespace XunitCustomFramework;

public class LoadTestFramework : XunitTestFramework
{
    public LoadTestFramework(IMessageSink messageSink)
        : base(messageSink)
    {
        messageSink.OnMessage(new DiagnosticMessage("Using CustomTestFramework"));
    }

    protected override ITestFrameworkExecutor CreateExecutor(AssemblyName assemblyName)
        => new CustomExecutor(assemblyName, SourceInformationProvider, DiagnosticMessageSink);

    private class CustomExecutor : XunitTestFrameworkExecutor
    {
        public CustomExecutor(AssemblyName assemblyName, ISourceInformationProvider sourceInformationProvider, IMessageSink diagnosticMessageSink)
            : base(assemblyName, sourceInformationProvider, diagnosticMessageSink)
        {
        }

        protected override async void RunTestCases(IEnumerable<IXunitTestCase> testCases, IMessageSink executionMessageSink, ITestFrameworkExecutionOptions executionOptions)
        {
            using var assemblyRunner = new CustomAssemblyRunner(TestAssembly, testCases, DiagnosticMessageSink, executionMessageSink, executionOptions);
            await assemblyRunner.RunAsync();
        }
    }

    private class CustomAssemblyRunner : XunitTestAssemblyRunner
    {
        public CustomAssemblyRunner(ITestAssembly testAssembly, IEnumerable<IXunitTestCase> testCases, IMessageSink diagnosticMessageSink, IMessageSink executionMessageSink, ITestFrameworkExecutionOptions executionOptions)
            : base(testAssembly, testCases, diagnosticMessageSink, executionMessageSink, executionOptions)
        {
        }

        protected override Task<RunSummary> RunTestCollectionAsync(IMessageBus messageBus, ITestCollection testCollection, IEnumerable<IXunitTestCase> testCases, CancellationTokenSource cancellationTokenSource)
            => new CustomTestCollectionRunner(testCollection, testCases, DiagnosticMessageSink, messageBus, TestCaseOrderer, new ExceptionAggregator(Aggregator), cancellationTokenSource).RunAsync();
    }

    private class CustomTestCollectionRunner : XunitTestCollectionRunner
    {
        public CustomTestCollectionRunner(ITestCollection testCollection, IEnumerable<IXunitTestCase> testCases, IMessageSink diagnosticMessageSink, IMessageBus messageBus, ITestCaseOrderer testCaseOrderer, ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource)
            : base(testCollection, testCases, diagnosticMessageSink, messageBus, testCaseOrderer, aggregator, cancellationTokenSource)
        {
        }

        protected override Task<RunSummary> RunTestClassAsync(ITestClass testClass, IReflectionTypeInfo @class, IEnumerable<IXunitTestCase> testCases)
            => new CustomTestClassRunner(testClass, @class, testCases, DiagnosticMessageSink, MessageBus, TestCaseOrderer, new ExceptionAggregator(Aggregator), CancellationTokenSource, CollectionFixtureMappings)
                .RunAsync();
    }

    private class CustomTestClassRunner : XunitTestClassRunner
    {
        public CustomTestClassRunner(ITestClass testClass, IReflectionTypeInfo @class, IEnumerable<IXunitTestCase> testCases, IMessageSink diagnosticMessageSink, IMessageBus messageBus, ITestCaseOrderer testCaseOrderer, ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource, IDictionary<Type, object> collectionFixtureMappings)
            : base(testClass, @class, testCases, diagnosticMessageSink, messageBus, testCaseOrderer, aggregator, cancellationTokenSource, collectionFixtureMappings)
        {
        }

        protected override Task<RunSummary> RunTestMethodAsync(ITestMethod testMethod, IReflectionMethodInfo method, IEnumerable<IXunitTestCase> testCases, object[] constructorArguments)
            => new CustomTestMethodRunner(testMethod, this.Class, method, testCases, this.DiagnosticMessageSink, this.MessageBus, new ExceptionAggregator(this.Aggregator), this.CancellationTokenSource, constructorArguments)
                .RunAsync();
    }

    private class CustomTestMethodRunner : XunitTestMethodRunner
    {
        private readonly IMessageSink _diagnosticMessageSink;

        public CustomTestMethodRunner(ITestMethod testMethod, IReflectionTypeInfo @class, IReflectionMethodInfo method, IEnumerable<IXunitTestCase> testCases, IMessageSink diagnosticMessageSink, IMessageBus messageBus, ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource, object[] constructorArguments)
            : base(testMethod, @class, method, testCases, diagnosticMessageSink, messageBus, aggregator, cancellationTokenSource, constructorArguments)
        {
            _diagnosticMessageSink = diagnosticMessageSink;
        }

        protected override async Task<RunSummary> RunTestCaseAsync(IXunitTestCase testCase)
        {
            var loadTestSettings = TestMethod.TestClass.Class.GetCustomAttributes(typeof(LoadTestSettingsAttribute))
                                       .FirstOrDefault()
                                   ?? TestMethod.Method.GetCustomAttributes(typeof(LoadTestSettingsAttribute))
                                       .FirstOrDefault();

            LoadSettings settings = null;
            if (loadTestSettings != null)
            {
                var properties = loadTestSettings.GetType().GetProperties();
                var settingsAttribute = properties.First(x => x.GetValue(loadTestSettings) is LoadTestSettingsAttribute).GetValue(loadTestSettings) as LoadTestSettingsAttribute;
                settings = new LoadSettings
                {
                    Concurrency = settingsAttribute.Concurrency,
                    Duration = TimeSpan.FromSeconds(settingsAttribute.DurationInSeconds),
                    Interval = TimeSpan.FromSeconds(settingsAttribute.IntervalInSeconds),
                };
            }

            var parameters = string.Empty;

            if (testCase.TestMethodArguments != null)
            {
                parameters = string.Join(", ", testCase.TestMethodArguments.Select(a => a?.ToString() ?? "null"));
            }

            var test = $"{TestMethod.TestClass.Class.Name}.{TestMethod.Method.Name}({parameters})";

            _diagnosticMessageSink.OnMessage(new DiagnosticMessage($"STARTED: {test}"));

            var deadline = TimeSpan.FromMinutes(2);
            using var timer = new Timer(
                _ => _diagnosticMessageSink.OnMessage(new DiagnosticMessage($"WARNING: {test} has been running for more than {deadline.TotalMinutes:N0} minutes")),
                null,
                deadline,
                Timeout.InfiniteTimeSpan);

            try
            {
                if (loadTestSettings != null)
                {
                    var action = async () => await base.RunTestCaseAsync(testCase);
                    var executionPlan = new LoadExecutionPlan
                    {
                        Name = testCase.DisplayName,
                        Action = async () =>
                        {
                            await action();
                            return true;
                        },
                        Settings = settings
                    };

                
                    var loadResult = await LoadRunner.Run(executionPlan);

                    var status = loadResult.Failure > 0
                        ? "FAILURE"
                        : "SUCCESS";

                    _diagnosticMessageSink.OnMessage(new DiagnosticMessage($"{status}: {test} ({loadResult.Time}s)"));

                    return new RunSummary()
                    {
                        Total = loadResult.Total,
                        Failed = loadResult.Failure,
                        Skipped = loadResult.Total - loadResult.Failure,
                        Time = loadResult.Time
                    };
                }

                var result = await base.RunTestCaseAsync(testCase);
                return result;

            }
            catch (Exception ex)
            {
                _diagnosticMessageSink.OnMessage(new DiagnosticMessage($"ERROR: {test} ({ex.Message})"));
                throw;
            }
        }
    }
}