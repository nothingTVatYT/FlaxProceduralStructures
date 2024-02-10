using System.Collections.Generic;

namespace ExtensionMethods {
    public static class ListExtensions {
        public static string Elements<T>(this List<T> list) {
            var result = "";
            foreach (var item in list) {
                if (result != "") result += ",";
                result += item.ToString();
            }
            return result;
        }

        public static string Elements<T>(this HashSet<T> list) {
            var result = "";
            foreach (var item in list) {
                if (result != "") result += ",";
                result += item.ToString();
            }
            return result;
        }

        public static T RandomItem<T>(this HashSet<T> set) {
            using var enumerator = set.GetEnumerator();
            enumerator.MoveNext();
            return enumerator.Current;
        }
    }
}
