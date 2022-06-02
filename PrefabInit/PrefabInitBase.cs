using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;


public class PrefabInitBase
{
    [MenuItem("Tools/Initialize Prefabs")]
    public static void Execute() {
        //Singleton:
        GameObject paramObject = GameObject.Find("Prefab Initialization Parameters");

        List<GameObject> modelParents = new();
        List<GameObject> models = new();

        string uninitiatedPath = "Assets/Model Side/Prefabs/Uninitiated/Resources";
        string initiatedPath = "Assets/Model Side/Prefabs/Initiated";

        string[] files = Directory.GetFiles(uninitiatedPath);
        foreach (string file in files){
            if (Path.GetExtension(file) == ".PREFAB" || Path.GetExtension(file) == ".prefab"){
                GameObject current = Resources.Load<GameObject>("Just_pants");
                modelParents.Add(current);
            }
        }
        Debug.Log("Initializing " + modelParents.Count + " meshes");
        
        foreach(GameObject modelParent in modelParents){
            GameObject instance = GameObject.Instantiate(modelParent);

            //Highlight Map Baking:
            //Channel 5 is for highlight map
            //================================================================================
            

            List<MeshFilter> filters = new();
            filters.AddRange(instance.GetComponentsInChildren<MeshFilter>());

            //Debug
            if(filters.Count < 1){Debug.LogError("No Meshfilters in prefab!");}

            int i = 0;
            foreach(MeshFilter filter in filters){
                Mesh mesh = filter.sharedMesh;
                Vector2[] highlightMap = BakeHighlightMaps.Bake(mesh, paramObject.GetComponent<PrefabInitParameters>());
                //Highlight values go to UV4
                mesh.SetUVs(4, highlightMap);

                //Generate Texture and write to file
                Texture2D texture = HighlightMapDrawUV.DrawUVMap(paramObject.GetComponent<PrefabInitParameters>(), highlightMap, mesh);
                byte[] bytes = texture.EncodeToPNG();
                File.WriteAllBytes(initiatedPath + "/HighlighMap" + i.ToString() + ".png", bytes);

                filter.gameObject.AddComponent<HighlightAngleUpdate>();

                MeshRenderer renderer = filter.gameObject.GetComponent<MeshRenderer>();
                renderer.material = paramObject.GetComponent<PrefabInitParameters>().highlightMat;
                renderer.material.SetTexture("_MainTex", texture);

                Debug.Log("Complete!");
                i++;
            }
            //================================================================================


            //End loop
            string localPath = initiatedPath + "/" + instance.name + ".prefab";
            localPath = AssetDatabase.GenerateUniqueAssetPath(localPath);
            bool succes;
            PrefabUtility.SaveAsPrefabAsset(instance, localPath, out succes);

            Object.DestroyImmediate(instance);
            Debug.Log(succes);
        }
    }
}