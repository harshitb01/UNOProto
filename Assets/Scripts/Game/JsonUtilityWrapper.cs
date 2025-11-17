using System;
using System.Collections.Generic;
public static class JsonUtilityWrapper {
    [Serializable]
    private class Wrapper<T> { public List<T> items; }

    public static List<T> FromJsonList<T>(string json) {
        // wrap into { "items": ... } then parse
        string wrapped = "{\"items\":" + json + "}";
        var w = UnityEngine.JsonUtility.FromJson<Wrapper<T>>(wrapped);
        return w.items ?? new List<T>();
    }

    public static string ToJsonList<T>(List<T> items) {
        var w = new Wrapper<T> { items = items };
        return UnityEngine.JsonUtility.ToJson(w);
    }
}
