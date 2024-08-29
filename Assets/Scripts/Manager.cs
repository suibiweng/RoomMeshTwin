using System.Collections;
using System.Collections.Generic;
using System.IO;
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

    public List<GameObject> cropboxes;

    void Start()
    {
        cropboxes = new List<GameObject>();

        string jsonPath = JsonPath;
        string jsonData = System.IO.File.ReadAllText(jsonPath);

        // Deserializing JSON with custom converters
        List<CropBoxData> cropBoxList = JsonConvert.DeserializeObject<List<CropBoxData>>(jsonData, new JsonSerializerSettings
        {
            Converters = { new Vector3Converter(), new QuaternionConverter() }
        });

        foreach (var cropBox in cropBoxList)
        {
            CreateCropBox(cropBox.urlid, cropBox.position, cropBox.rotation, cropBox.scale * 2.3f);
        }

        // ExportCroppedMeshes();
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

        cropboxes.Add(g);
    }

    void ExportCroppedMeshes()
    {
        foreach (var cropBox in cropboxes)
        {
            Mesh croppedMesh = CropMesh(RoomMesh.GetComponent<MeshFilter>().mesh, cropBox);

            if (croppedMesh != null)
            {
                SaveMeshAsOBJ(croppedMesh, cropBox.name);
            }
        }
    }

    Mesh CropMesh(Mesh originalMesh, GameObject cropBox)
    {
        Vector3[] originalVertices = originalMesh.vertices;
        int[] originalTriangles = originalMesh.triangles;
        List<Vector3> newVertices = new List<Vector3>();
        List<int> newTriangles = new List<int>();
        Dictionary<int, int> vertexMap = new Dictionary<int, int>();

        // Get the bounding box in world space
        Bounds cropBounds = new Bounds(cropBox.transform.position, cropBox.transform.localScale);

        // Iterate through each triangle in the mesh
        for (int i = 0; i < originalTriangles.Length; i += 3)
        {
            // Get the vertices for this triangle
            Vector3 v1 = originalVertices[originalTriangles[i]];
            Vector3 v2 = originalVertices[originalTriangles[i + 1]];
            Vector3 v3 = originalVertices[originalTriangles[i + 2]];

            // Transform vertices to world space
            v1 = RoomMesh.transform.TransformPoint(v1);
            v2 = RoomMesh.transform.TransformPoint(v2);
            v3 = RoomMesh.transform.TransformPoint(v3);

            // Check if all vertices of the triangle are within the bounds of the crop box
            if (cropBounds.Contains(v1) && cropBounds.Contains(v2) && cropBounds.Contains(v3))
            {
                // For each vertex, either reuse an existing one or add a new one
                for (int j = 0; j < 3; j++)
                {
                    int originalIndex = originalTriangles[i + j];
                    Vector3 vertex = originalVertices[originalIndex];
                    if (!vertexMap.ContainsKey(originalIndex))
                    {
                        newVertices.Add(vertex);
                        vertexMap[originalIndex] = newVertices.Count - 1;
                    }
                    newTriangles.Add(vertexMap[originalIndex]);
                }
            }
        }

        // Create a new mesh from the cropped vertices and triangles
        Mesh croppedMesh = new Mesh();
        croppedMesh.vertices = newVertices.ToArray();
        croppedMesh.triangles = newTriangles.ToArray();
        croppedMesh.RecalculateNormals(); // Optional, recalculate normals if needed

        return croppedMesh;
    }

    void SaveMeshAsOBJ(Mesh mesh, string name)
    {
        string path = Path.Combine(Application.dataPath, "ExportedMeshes", name + ".obj");
        using (StreamWriter sw = new StreamWriter(path))
        {
            sw.Write(MeshToString(mesh));
        }
        Debug.Log("Mesh saved as OBJ at: " + path);
    }

    string MeshToString(Mesh mesh)
    {
        StringWriter sw = new StringWriter();

        sw.WriteLine("# Exported Mesh");
        foreach (Vector3 v in mesh.vertices)
        {
            sw.WriteLine(string.Format("v {0} {1} {2}", v.x, v.y, v.z));
        }
        sw.WriteLine();

        foreach (Vector3 v in mesh.normals)
        {
            sw.WriteLine(string.Format("vn {0} {1} {2}", v.x, v.y, v.z));
        }
        sw.WriteLine();

        foreach (Vector2 v in mesh.uv)
        {
            sw.WriteLine(string.Format("vt {0} {1}", v.x, v.y));
        }
        sw.WriteLine();

        for (int i = 0; i < mesh.subMeshCount; i++)
        {
            int[] triangles = mesh.GetTriangles(i);
            for (int j = 0; j < triangles.Length; j += 3)
            {
                sw.WriteLine(string.Format("f {0} {1} {2}",
                    triangles[j] + 1, triangles[j + 1] + 1, triangles[j + 2] + 1));
            }
        }

        return sw.ToString();
    }

    void Update()
    {
        // Any necessary updates can be handled here

        if(Input.GetKeyDown(KeyCode.Space)){


            ExportCroppedMeshes();



        }

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
