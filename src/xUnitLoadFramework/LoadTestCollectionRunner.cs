// using System.Collections.Generic;
// using System.Threading;
// using System.Threading.Tasks;
// using Xunit.Abstractions;
// using Xunit.Sdk;
//
// namespace xUnitLoadFramework
// {
//     public class LoadTestCollectionRunner : XunitTestCollectionRunner
//     {
//         public LoadTestCollectionRunner(
//             ITestCollection testCollection,
//             IEnumerable<IXunitTestCase> testCases,
//             IMessageSink diagnosticMessageSink,
//             IMessageBus messageBus,
//             ITestCaseOrderer testCaseOrderer,
//             ExceptionAggregator aggregator,
//             CancellationTokenSource cancellationTokenSource)
//             : base(testCollection, testCases, diagnosticMessageSink, messageBus, testCaseOrderer, aggregator, cancellationTokenSource)
//         {
//         }
//
//         protected override Task<RunSummary> RunTestClassAsync(
//             ITestClass testClass,
//             IReflectionTypeInfo @class,
//             IEnumerable<IXunitTestCase> testCases)
//         {
//             return new LoadTestClassRunner(
//                 testClass,
//                 @class,
//                 testCases,
//                 DiagnosticMessageSink,
//                 MessageBus,
//                 TestCaseOrderer,
//                 new ExceptionAggregator(Aggregator),
//                 CancellationTokenSource,
//                 CollectionFixtureMappings).RunAsync();
//         }
//     }
// }