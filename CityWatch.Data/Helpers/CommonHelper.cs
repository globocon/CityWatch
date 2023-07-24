using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace CityWatch.Data.Helpers
{
    public static class CommonHelper
    {
        public static bool IsValidEmail(string email)
        {
            var trimmedEmail = email.Trim();
            if (trimmedEmail.EndsWith("."))
                return false;

            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == trimmedEmail;
            }
            catch
            {
                return false;
            }
        }
    }

    public static class DateTimeExtensions
    {
        public static DateTime UtcToAest(this DateTime utcDateTime)
        {
            return utcDateTime.AddHours(10);
        }
    }

    public static class EnumExtensions
    {
        public static string ToDescription(this Enum value)
        {
            Type type = value.GetType();
            string name = Enum.GetName(type, value);
            if (name != null)
            {
                FieldInfo field = type.GetField(name);
                if (field != null)
                {
                    DescriptionAttribute attr = Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) as DescriptionAttribute;
                    if (attr != null)
                    {
                        return attr.Description;
                    }
                }
            }
            return null;
        }

        public static string ToDisplayName(this Enum value)
        {
            Type type = value.GetType();
            string name = Enum.GetName(type, value);
            if (name != null)
            {
                FieldInfo field = type.GetField(name);
                if (field != null)
                {
                    DisplayAttribute attr = Attribute.GetCustomAttribute(field, typeof(DisplayAttribute)) as DisplayAttribute;
                    if (attr != null)
                    {
                        return attr.GetName();
                    }
                }
            }
            return name;
        }
    }    
}
