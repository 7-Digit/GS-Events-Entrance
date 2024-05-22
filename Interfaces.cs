using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

public static class JsonStorage
{
    private static readonly string filePath = "localdata.json";

    public static void SaveData<T>(T data)
    {
        string json = JsonConvert.SerializeObject(data, Formatting.Indented);
        File.WriteAllText(filePath, json);
    }

    public static T LoadData<T>()
    {
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<T>(json);
        }
        return default(T);
    }
}
