using System.ComponentModel;
using System.Reflection;

namespace Tracker.Extensions
{
    public static class EnumExtensions
    {
        /// <summary>
        /// Retrieves the Description attribute value of an enum member.
        /// </summary>
        /// <param name="value">The enum value.</param>
        /// <returns>The Description attribute value if present; otherwise, the enum member name as a string.</returns>
        public static string ToDescriptionString(this Enum value)
        { 
            FieldInfo field = value.GetType().GetField(value.ToString()); 

            DescriptionAttribute attribute = field.GetCustomAttribute<DescriptionAttribute>();

            if (attribute != null)
            {
                return attribute.Description;
            }

            return value.ToString();
        }
    }
}
