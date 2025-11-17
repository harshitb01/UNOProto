using Newtonsoft.Json;
using System.Collections.Generic;

public static class NewtonsoftJsonWrapper {
    public static string Serialize(object o) => JsonConvert.SerializeObject(o);
    public static T Deserialize<T>(string s) => JsonConvert.DeserializeObject<T>(s);

    public static int[] ConvertToIntArray(object arrObj) {
        var list = JsonConvert.DeserializeObject<int[]>(JsonConvert.SerializeObject(arrObj));
        return list;
    }
}
