using Xunit.Internal;
using Xunit.Sdk;
using Xunit.v3;

namespace xUnitV3LoadFramework.Extensions.ObjectModel;

public class LoadCollection(
    LoadTestAssembly testAssembly,
    string displayName) :
        ITestCollection
{
    public LoadTestAssembly TestAssembly { get; } =
        Guard.ArgumentNotNull(testAssembly);

    ITestAssembly ITestCollection.TestAssembly =>
        TestAssembly;

    public string? TestCollectionClassName =>
        null;

    public string TestCollectionDisplayName =>
        Guard.ArgumentNotNull(displayName);

    public IReadOnlyDictionary<string, IReadOnlyCollection<string>> Traits { get; } =
        ExtensibilityPointFactory.GetCollectionTraits(testCollectionDefinition: null, testAssembly.Traits);

    public string UniqueID { get; } =
        UniqueIDGenerator.ForTestCollection(testAssembly.UniqueID, displayName, collectionDefinitionClassName: null);
}
