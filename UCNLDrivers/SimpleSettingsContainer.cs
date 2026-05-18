using System.Collections;
using System.Reflection;
using System.Text;

namespace UCNLDrivers
{
    [Serializable]
    public abstract class SimpleSettingsContainer
    {
        public SimpleSettingsContainer()
        {
            SetDefaults();
        }

        #region Methods

        public abstract void SetDefaults();

        private string FormatValue(object value)
        {
            if (value == null) return "null";

            if (value is Array array)
            {
                return string.Join(", ", array.Cast<object>().Select(FormatValue));
            }
            else if (value is System.Collections.IList list)
            {
                if (list.Count == 0) return "(empty)";
                var items = new List<string>();
                foreach (var item in list)
                    items.Add(FormatValue(item));
                return $"[{string.Join(", ", items)}]";
            }
            else if (IsGenericDictionary(value))
            {
                return FormatGenericDictionary(value);
            }
            else if (value is IDictionary nonGenericDict)
            {
                if (nonGenericDict.Count == 0) return "(empty)";
                var pairs = nonGenericDict.Cast<DictionaryEntry>()
                    .Select(entry => $"[{FormatValue(entry.Key)}={FormatValue(entry.Value)}]");
                return string.Join(", ", pairs);
            }
            else if (value.GetType().IsClass && !IsSystemType(value.GetType()))
            {
                return value.ToString() ?? "null";
            }
            else
            {
                return value.ToString() ?? "null";
            }
        }

        private static bool IsGenericDictionary(object value)
        {
            var type = value.GetType();
            return type.IsGenericType &&
                   type.GetGenericTypeDefinition() == typeof(Dictionary<,>);
        }

        private static bool IsSystemType(Type type)
        {
            return type.Namespace?.StartsWith("System") == true;
        }

        private string FormatGenericDictionary(object dictionary)
        {
            try
            {
                var dictType = dictionary.GetType();
                var keyValuePairs = dictType.GetProperty("Keys")?.GetValue(dictionary) as System.Collections.IEnumerable;
                var values = dictType.GetProperty("Values")?.GetValue(dictionary) as System.Collections.IEnumerable;

                if (keyValuePairs == null || values == null) return "(invalid dictionary)";

                var keys = keyValuePairs.Cast<object>().ToList();
                var vals = values.Cast<object>().ToList();

                var pairs = new List<string>();
                for (int i = 0; i < keys.Count; i++)
                {
                    pairs.Add($"[{FormatValue(keys[i])}={FormatValue(vals[i])}]");
                }

                return pairs.Count > 0 ? string.Join(", ", pairs) : "(empty)";
            }
            catch
            {
                return "(error formatting dictionary)";
            }
        }


        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine();

            // Поля (исключаем backing fields автосвойств)
            var fields = this.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Where(f => !f.Name.Contains("k__BackingField"));
            foreach (var field in fields)
            {
                var value = field.GetValue(this);
                sb.AppendFormat("-- {0}: {1}\r\n", field.Name, FormatValue(value));
            }

            // Свойства (только публичные, с Get-методом)
            var properties = this.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && !p.GetIndexParameters().Any());
            foreach (var prop in properties)
            {
                var value = prop.GetValue(this);
                sb.AppendFormat("-- {0}: {1}\r\n", prop.Name, FormatValue(value));
            }

            return sb.ToString();
        }


        #endregion
    }
}
