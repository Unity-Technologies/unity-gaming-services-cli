using System.Reflection;

namespace Unity.Services.Cli.Common;

static class ReflectionHelper
{
    public static void SetMemberValue(this MemberInfo self, object? instance, object? value)
    {
        switch (self)
        {
            case FieldInfo field:
                field.SetValue(instance, value);
                break;
            case PropertyInfo property:
                property.SetValue(instance, value);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(self));
        }
    }

    public static Type GetMemberType(this MemberInfo self)
        => self switch
        {
            FieldInfo field => field.FieldType,
            PropertyInfo property => property.PropertyType,
            _ => throw new ArgumentOutOfRangeException(nameof(self))
        };

    public static object? GetMemberValue(this MemberInfo self, object? instance)
        => self switch
        {
            FieldInfo field => field.GetValue(instance),
            PropertyInfo property => property.GetValue(instance),
            _ => throw new ArgumentOutOfRangeException(nameof(self))
        };
}
