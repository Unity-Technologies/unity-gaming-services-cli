using Newtonsoft.Json;
using NUnit.Framework;
using Unity.Services.Cli.Matchmaker.Parser;
using System.Reflection;

namespace Unity.Services.Cli.Matchmaker.UnitTest
{
    [TestFixture]
    class FilterValueConverterTests
    {
        JsonSerializerSettings m_Settings = null!;

        [SetUp]
        public void Setup()
        {
            m_Settings = new JsonSerializerSettings
            {
                Converters = { new FilterValueConverter() }
            };
        }

        object CreateFilterValue(string json)
        {
            var filteredPoolConfigType = typeof(Unity.Services.Multiplayer.Editor.Matchmaker.Authoring.Core.Model.FilteredPoolConfig);
            var filterType = filteredPoolConfigType.GetNestedType("Filter", BindingFlags.Public | BindingFlags.NonPublic);
            var filterValueType = filterType?.GetNestedType("FilterValue", BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(filterValueType, "FilterValue type not found via reflection");
            var result = JsonConvert.DeserializeObject(json, filterValueType!, m_Settings);
            Assert.IsNotNull(result, "Deserialization returned null");
            return result!;
        }

        static string ExtractPrimitive(object fv)
        {
            var t = fv.GetType();
            foreach (var prop in t.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (!prop.CanRead) continue;
                var val = prop.GetValue(fv);
                if (val == null) continue;
                var pt = prop.PropertyType;
                if (pt == typeof(string) || pt == typeof(int) || pt == typeof(float) || pt == typeof(double))
                    return val.ToString()!;
            }
            foreach (var field in t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                var val = field.GetValue(fv);
                if (val == null) continue;
                var ft = field.FieldType;
                if (ft == typeof(string) || ft == typeof(int) || ft == typeof(float) || ft == typeof(double))
                    return val.ToString()!;
            }
            return fv.ToString() ?? string.Empty;
        }

        [Test]
        public void Deserialize_Int()
        {
            var fv = CreateFilterValue("2");
            Assert.AreEqual("2", ExtractPrimitive(fv));
        }

        [Test]
        public void Deserialize_Float()
        {
            var fv = CreateFilterValue("10.5");
            Assert.AreEqual("10.5", ExtractPrimitive(fv));
        }

        [Test]
        public void Deserialize_String()
        {
            var fv = CreateFilterValue("\"test\"");
            Assert.AreEqual("test", ExtractPrimitive(fv));
        }

        [Test]
        public void Serialize_Number_Unquoted()
        {
            var fv = CreateFilterValue("10.5");
            var json = JsonConvert.SerializeObject(fv, m_Settings);
            Assert.AreEqual("10.5", json);
        }

        [Test]
        public void Serialize_String_Quoted()
        {
            var fv = CreateFilterValue("\"mode\"");
            var json = JsonConvert.SerializeObject(fv, m_Settings);
            Assert.AreEqual("\"mode\"", json);
        }
    }
}
