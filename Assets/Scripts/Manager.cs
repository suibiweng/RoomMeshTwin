using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class CropBoxData
{
    public string urlid;
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 scale;
}

public class Manager : MonoBehaviour
{
    public OSC osc;
    public GameObject RoomMesh;
    public GameObject CropBox;
    public string JsonPath;

    void Start()
    {
        string jsonPath = JsonPath;
        string jsonData = System.IO.File.ReadAllText(jsonPath);

        // Deserializing JSON with custom converters
        List<CropBoxData> cropBoxList = JsonConvert.DeserializeObject<List<CropBoxData>>(jsonData, new JsonSerializerSettings
        {
            Converters = { new Vector3Converter(), new QuaternionConverter() }
        });

        foreach (var cropBox in cropBoxList)
        {
            CreateCropBox(cropBox.urlid, cropBox.position, cropBox.rotation, cropBox.scale);
        }
    }

    void receiveCropBoxes(OscMessage message)
    {
        if (message.values.Count < 4)
        {
            Debug.LogError("OSC message does not have enough values.");
            return;
        }

        try
        {
            Vector3 position = (Vector3)message.values[1];
            Quaternion rotation = (Quaternion)message.values[2];
            Vector3 scale = (Vector3)message.values[3];
            string urlid = message.values[0].ToString();

            CreateCropBox(urlid, position, rotation, scale);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Error processing OSC message: " + ex.Message);
        }
    }

    void CreateCropBox(string urlid, Vector3 Position, Quaternion Rotation, Vector3 LocalSize)
    {
        GameObject g = Instantiate(CropBox, Position, Rotation);
        g.transform.localScale = LocalSize;
        g.name = urlid;
    }

    void Update()
    {
        // Any necessary updates can be handled here
    }

    // Custom converter for Vector3
    public class Vector3Converter : JsonConverter<Vector3>
    {
        public override Vector3 ReadJson(JsonReader reader, System.Type objectType, Vector3 existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JArray array = JArray.Load(reader);
            return new Vector3(array[0].Value<float>(), array[1].Value<float>(), array[2].Value<float>());
        }

        public override void WriteJson(JsonWriter writer, Vector3 value, JsonSerializer serializer)
        {
            writer.WriteStartArray();
            writer.WriteValue(value.x);
            writer.WriteValue(value.y);
            writer.WriteValue(value.z);
            writer.WriteEndArray();
        }
    }

    // Custom converter for Quaternion
    public class QuaternionConverter : JsonConverter<Quaternion>
    {
        public override Quaternion ReadJson(JsonReader reader, System.Type objectType, Quaternion existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JArray array = JArray.Load(reader);
            return new Quaternion(array[0].Value<float>(), array[1].Value<float>(), array[2].Value<float>(), array[3].Value<float>());
        }

        public override void WriteJson(JsonWriter writer, Quaternion value, JsonSerializer serializer)
        {
            writer.WriteStartArray();
            writer.WriteValue(value.x);
            writer.WriteValue(value.y);
            writer.WriteValue(value.z);
            writer.WriteValue(value.w);
            writer.WriteEndArray();
        }
    }
}
