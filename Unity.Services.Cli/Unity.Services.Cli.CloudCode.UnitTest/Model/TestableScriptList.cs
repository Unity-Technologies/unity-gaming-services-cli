using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Services.CloudCode.Authoring.Editor.Core.Model;

namespace Unity.Services.Cli.CloudCode.UnitTest;

class TestableScriptList : IReadOnlyList<IScript>
{
    public IReadOnlyList<IScript> All { get; }

    public IReadOnlyList<IScript> Expected { get; }

    public IReadOnlyList<IScript> Failed { get; }

    public TestableScriptList(IReadOnlyList<IScript> expected, IReadOnlyList<IScript> failed)
    {
        Expected = expected;
        Failed = failed;
        All = expected.Union(failed).ToList();
    }

    public IEnumerator<IScript> GetEnumerator()
    {
        return All.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)All).GetEnumerator();
    }

    public int Count => All.Count;

    public IScript this[int index] => All[index];
}
