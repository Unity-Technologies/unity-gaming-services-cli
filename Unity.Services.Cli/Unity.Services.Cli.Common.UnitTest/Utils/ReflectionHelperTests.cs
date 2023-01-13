using NUnit.Framework;

namespace Unity.Services.Cli.Common.UnitTest.Utils;

[TestFixture]
class ReflectionHelperTests
{
    class ReflectionDummy
    {
        public event Action? Foo;

        public bool Flag;

        public bool FlagProperty
        {
            get => Flag;
            set => Flag = value;
        }

        public void RaiseFoo() => Foo?.Invoke();
    }

    [Test]
    public void SetMemberValueOnFieldSucceeds()
    {
        var instance = new ReflectionDummy();
        var member = typeof(ReflectionDummy).GetField(nameof(ReflectionDummy.Flag))!;

        member.SetMemberValue(instance, true);

        Assert.IsTrue(instance.Flag);
    }

    [Test]
    public void SetMemberValueOnPropertySucceeds()
    {
        var instance = new ReflectionDummy();
        var member = typeof(ReflectionDummy).GetProperty(nameof(ReflectionDummy.FlagProperty))!;

        member.SetMemberValue(instance, true);

        Assert.IsTrue(instance.FlagProperty);
    }

    [TestCase(nameof(ReflectionDummy.RaiseFoo))]
    [TestCase(nameof(ReflectionDummy.Foo))]
    public void SetMemberValueOnNonFieldNorPropertyMemberThrows(string memberName)
    {
        var instance = new ReflectionDummy();
        var member = typeof(ReflectionDummy).GetMember(memberName).First();

        Assert.Throws<ArgumentOutOfRangeException>(() => member.SetMemberValue(instance, null));
    }

    [Test]
    public void GetMemberValueOnFieldSucceeds()
    {
        var instance = new ReflectionDummy
        {
            Flag = true,
        };
        var member = typeof(ReflectionDummy).GetField(nameof(ReflectionDummy.Flag))!;

        var value = (bool)(member.GetMemberValue(instance) ?? false);

        Assert.IsTrue(value);
    }

    [Test]
    public void GetMemberValueOnPropertySucceeds()
    {
        var instance = new ReflectionDummy
        {
            FlagProperty = true,
        };
        var member = typeof(ReflectionDummy).GetProperty(nameof(ReflectionDummy.FlagProperty))!;

        var value = (bool)(member.GetMemberValue(instance) ?? false);

        Assert.IsTrue(value);
    }

    [TestCase(nameof(ReflectionDummy.RaiseFoo))]
    [TestCase(nameof(ReflectionDummy.Foo))]
    public void GetMemberValueOnNonFieldNorPropertyMemberThrows(string memberName)
    {
        var instance = new ReflectionDummy();
        var member = typeof(ReflectionDummy).GetMember(memberName).First();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => member.GetMemberValue(instance));
    }

    [Test]
    public void GetMemberTypeOnFieldSucceeds()
    {
        var member = typeof(ReflectionDummy).GetField(nameof(ReflectionDummy.Flag))!;

        var type = member.GetMemberType();

        Assert.AreEqual(typeof(bool), type);
    }

    [Test]
    public void GetMemberTypeOnPropertySucceeds()
    {
        var member = typeof(ReflectionDummy).GetProperty(nameof(ReflectionDummy.FlagProperty))!;

        var type = member.GetMemberType();

        Assert.AreEqual(typeof(bool), type);
    }

    [TestCase(nameof(ReflectionDummy.RaiseFoo))]
    [TestCase(nameof(ReflectionDummy.Foo))]
    public void GetMemberTypeOnNonFieldNorPropertyMemberThrows(string memberName)
    {
        var member = typeof(ReflectionDummy).GetMember(memberName).First();

        Assert.Throws<ArgumentOutOfRangeException>(() => member.GetMemberType());
    }
}
