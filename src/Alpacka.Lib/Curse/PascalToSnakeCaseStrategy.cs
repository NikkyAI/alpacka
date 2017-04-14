using System.Text;
using Newtonsoft.Json.Serialization;

namespace Alpacka.Lib.Curse
{
    public class PascalToSnakeCaseStrategy : NamingStrategy
    {
        protected override string ResolvePropertyName(string name)
        {
            var str = new StringBuilder();
            for (var i = 0; i < name.Length; i++) {
                var c = name[i];
                if (char.IsUpper(c)) {
                    if (i > 0) {
                        var _c = name[i - 1];
                        if (char.IsLower(_c))
                            str.Append('_');
                    }
                    c = char.ToLower(c);
                }
                str.Append(c);
            }
            return str.ToString();
        }
    }
}