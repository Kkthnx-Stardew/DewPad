using System.Linq.Expressions;
using System.Reflection;

namespace DewPad.Prompts
{
    /// <summary>
    /// Reads a field the game keeps protected, through a getter compiled once when it is first needed. The reflection is done one time and a read
    /// afterwards is a plain delegate call that allocates nothing. A field that is missing or has another type, as a game update might leave it,
    /// gives no getter, so whatever depends on it quietly switches itself off.
    /// </summary>
    internal static class ProtectedFields
    {
        public static Func<TOwner, TValue>? Getter<TOwner, TValue>(string fieldName)
        {
            try
            {
                var field = typeof(TOwner).GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field is null || !typeof(TValue).IsAssignableFrom(field.FieldType))
                    return null;

                var owner = Expression.Parameter(typeof(TOwner), "owner");
                var body = Expression.Convert(Expression.Field(owner, field), typeof(TValue));
                return Expression.Lambda<Func<TOwner, TValue>>(body, owner).Compile();
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}