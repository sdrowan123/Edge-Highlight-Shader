using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class DebugVertices
{
    [MenuItem("Tools/Debug Vertices")]
    public static void Execute(){
        Debug.Log("Executing");
        GameObject parentObject = GameObject.Find("Just_pants(Clone)");
        foreach(MeshFilter filter in parentObject.GetComponentsInChildren<MeshFilter>()){
            var  localToWorld = filter.gameObject.transform.localToWorldMatrix;
            Mesh mesh = filter.sharedMesh;
            List<Vector2> uvs = new();
            mesh.GetUVs(4, uvs);
            Vector3[] poss = mesh.vertices;
            for(int i = 0; i < poss.Length; i++){
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.localScale = new Vector3(0.001f, 0.001f, 0.001f);
                cube.transform.rotation = Quaternion.Euler(0, uvs[i].x, 0);
                cube.transform.position = localToWorld.MultiplyPoint3x4(mesh.vertices[i]);
            }
        }
        Debug.Log("Done");
    }
}
