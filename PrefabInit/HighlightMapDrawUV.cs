using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HighlightMapDrawUV : MonoBehaviour
{

    struct QuadInfo{
        public int[] tri1;
        public int[] tri2;
    }
    //FUCK THIS, just interpolate over all vertices, why tf not.
    public static Texture2D DrawUVMap(PrefabInitParameters parameters, Vector2[] sharpnessMap, Mesh mesh){
        int[] triangles = mesh.triangles;
        Vector3[] vertices = mesh.vertices;
        Vector2[] uvs = mesh.uv;
        if(uvs.Length != vertices.Length){Debug.LogError("Uvs length does not match vertices length");}
        float debugAvgAlpha = 0;
        float debugAvgNormalizedDst = 0;
        float debugDstNum = 0;
        //Before doing anything, we'll make a dictionary of all quads for every vertex. This will make it easy for us to do bilinear interpolation.
        //Dictionary<int, List<QuadInfo>> quadDict = GenerateQuadDict(uvs, triangles);
        int size = parameters.textureSize;
        Texture2D texture = new Texture2D(size, size, TextureFormat.Alpha8, false);

        float maxDistance = Mathf.Sqrt((size * size) / uvs.Length * parameters.UVExpectedVertsDistance) / size;
        for(int x = 0; x < size; x++){
            for(int y = 0; y < size; y++){

                float alpha = 0;
                float num = 0;

                //Heres where you were. Alpha is being super funky, debugging really low while normalized dst seems decent.
                for(int i = 0; i < uvs.Length; i++){

                    Vector2 pos = PixelToUVCoord(new Vector2(x, y), size);
                    float dist = Vector2.Distance(pos, uvs[i]);

                    if(dist < maxDistance) {
                        alpha += (1 - dist / maxDistance) * sharpnessMap[i].x * (1 - dist / maxDistance);
                        debugAvgNormalizedDst += (1 - dist / maxDistance);
                        debugDstNum += (1 - dist / maxDistance);
                        num += (1 - dist / maxDistance);
                    }
                }

                if(num != 0) alpha = alpha / num;
                debugAvgAlpha += alpha;
                Color c = new Color(1f, 1f, 1f, alpha);
                texture.SetPixel(x, y, c);
            }
        }
        Debug.Log("Avg Alpha:" + debugAvgAlpha / (size * size));
        Debug.Log("Avg Distance:" + debugAvgNormalizedDst / debugDstNum);
        texture.Apply();
        return texture;
    }


    static Dictionary<int, List<QuadInfo>> GenerateQuadDict(Vector2[] uvs, int[] triangles){
        Dictionary<int, List<QuadInfo>> returnDict = new();
        for(int i = 0; i < uvs.Length; i++){
            
            for(int triangle = 0; triangle < triangles.Length; triangle += 3){
                
                if(triangles[triangle] == i || triangles[triangle + 1] == i || triangles[triangle + 2] == i){
                    QuadInfo currQuad = new QuadInfo();
                    int[] cI = new int[2]; //cI means coupleIndices
                    int index1 = triangle;

                    currQuad.tri1 = new int[] {triangles[triangle], triangles[triangle + 1], triangles[triangle + 2]};
                    
                    //Finally, add the quad to the dictionary
                    if(!returnDict.ContainsKey(i)){returnDict[i] = new List<QuadInfo>();}
                    returnDict[i].Add(currQuad);
                }    
            }
        }
        return returnDict;
    }

    static Vector2 UVCoordToPixel(Vector2 UVCoord, float imageSize){
        return UVCoord * imageSize;
    }

    static Vector2 PixelToUVCoord(Vector2 pixelCoord, float imageSize){
        return pixelCoord / imageSize;
    }

    /*static bool WithinQuad(Vector2 pixelCoord, QuadInfo quad, Vector2[] UVs, float imageSize){
        Vector2 UVCoord = PixelToUVCoord(pixelCoord, imageSize);
        bool bool1 = false;
        bool bool2 = false;

        for(int i = 0; i < 2; i++){
            float d1, d2, d3;
            bool has_neg, has_pos;
            int[] tris;
            if (i == 0){tris = quad.tri1;}
            else if(IsZero(quad.tri2)) {tris = quad.tri2;}
            else{break;}
            d1 = Sign(UVCoord, UVs[tris[0]], UVs[tris[1]]);
            d2 = Sign(UVCoord, UVs[tris[1]], UVs[tris[2]]);
            d3 = Sign(UVCoord, UVs[tris[2]], UVs[tris[0]]);

            has_neg = (d1 < 0) || (d2 < 0) || (d3 < 0);
            has_pos = (d1 > 0) || (d2 > 0) || (d3 > 0);
            
            if (i == 0){bool1 = !(has_neg && has_pos);}
            else{bool2 = !(has_neg && has_pos);}
        }

        return bool1 || bool2;
    }*/

    static bool WithinQuad(Vector2 pixelCoord, QuadInfo quad, Vector2[] UVs, float imageSize){
        bool t1 = PointInTriangle(PixelToUVCoord(pixelCoord, imageSize), UVs[quad.tri1[0]], UVs[quad.tri1[1]], UVs[quad.tri1[2]]);
        bool t2 = PointInTriangle(PixelToUVCoord(pixelCoord, imageSize), UVs[quad.tri2[0]], UVs[quad.tri2[1]], UVs[quad.tri2[2]]);

        Vector2[] uvs;
        if(IsZero(quad.tri2)){
            uvs = new Vector2[] {UVs[quad.tri1[0]], UVs[quad.tri1[1]], UVs[quad.tri1[2]], UVs[quad.tri2[0]]};
        }
        else{
            uvs = new Vector2[] {UVs[quad.tri1[0]], UVs[quad.tri1[1]], UVs[quad.tri1[2]]};
        }

        float[] maxDistances = new float[uvs.Length];
        for(int i = 0; i < uvs.Length; i++){
            for(int j = 0; j < uvs.Length; j++){
                if(i == j){continue;}
                float dist = Vector2.Distance(uvs[i], uvs[j]);
                if (dist > maxDistances[i]) maxDistances[i] = dist;
            }
        }

        //Then find point distance from each uv
        bool distanceWithinRange = true;
        for(int i = 0; i < uvs.Length; i++){
            float dist = Vector2.Distance(uvs[i], PixelToUVCoord(pixelCoord, imageSize));
            if(dist > maxDistances[i]) distanceWithinRange = false;
        }

        return (t1 || t2) && distanceWithinRange;
    }

    static bool SameSide(Vector2 p1, Vector2 p2, Vector2 a, Vector2 b){
        Vector2 cp1 = Vector3.Cross(b-a, p1-a);
        Vector2 cp2 = Vector3.Cross(b-a, p2-a);
        if (Vector2.Dot(cp1, cp2) >= 0) return true;
        else return false;
    }

    static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c){
        float side1 = (p.x - b.x) * (a.y - b.y) - (a.x - b.x) * (p.y - b.y);
        float side2 = (p.x - c.x) * (b.y - c.y) - (b.x - c.x) * (p.y - c.y);
        float side3 = (p.x - a.x) * (c.y - a.y) - (c.x - a.x) * (p.y - a.y);
        return (side1 < 0.0f) == (side2 < 0.0f) == (side3 < 0.0f);
    }

    public static float QuadLerp(Vector2[] uvs, float[] weights, Vector2 point, float imageSize){
        float[] distances = new float[4];
        point = PixelToUVCoord(point, imageSize);
        //First find max distance from each uv for normalization

        float[] maxDistances = new float[uvs.Length];
        for(int i = 0; i < uvs.Length; i++){
            for(int j = 0; j < uvs.Length; j++){
                if(i == j){continue;}
                float dist = Vector2.Distance(uvs[i], uvs[j]);
                if (dist > maxDistances[i]) maxDistances[i] = dist;
            }
        }

        //Then find point distance from each uv
        for(int i = 0; i < uvs.Length; i++){
            distances[i] = Vector2.Distance(uvs[i], point);
        }

        //normalize and multiply
        float prod = 1;
        for(int i = 0; i < uvs.Length; i++){
            float normalizedDist = distances[i] / maxDistances[i];
            //if(normalizedDist > 1){prod = -1; break;}
            prod *= normalizedDist * weights[i];
            //if(weights[i] > 1){prod = -2; break;}
        }

        //renormalize
        /*float absoluteMax = 1;
        foreach (float distance in maxDistances){
            absoluteMax *= distance;
        }
        prod = prod / absoluteMax;*/

        return prod * 1000;
    }

    public static float TriLerp(Vector2[] uvs, float[] weights, Vector2 point, float imageSize){
        float[] distances = new float[3];
        point = PixelToUVCoord(point, imageSize);
        //First find max distance from each uv for normalization

        float[] maxDistances = new float[uvs.Length];
        for(int i = 0; i < uvs.Length; i++){
            for(int j = 0; j < uvs.Length; j++){
                if(i == j){continue;}
                float dist = Vector2.Distance(uvs[i], uvs[j]);
                if (dist > maxDistances[i]) maxDistances[i] = dist;
            }
        }

        //Then find point distance from each uv
        for(int i = 0; i < uvs.Length; i++){
            distances[i] = Vector2.Distance(uvs[i], point);
        }

        //normalize and multiply
        float prod = 1;
        for(int i = 0; i < uvs.Length; i++){
            float normalizedDist = (distances[i]) / (maxDistances[i]);
            if(normalizedDist > 1){prod = -1; break;}
            prod *= normalizedDist * weights[i];
            if(weights[i] > 1){prod = -2; break;}
        }

        //renormalize
        /*float absoluteMax = 1;
        foreach (float distance in maxDistances){
            absoluteMax *= distance;
        }
        prod = prod / absoluteMax;*/

        return prod;
    }
    /*public static Vector2 QuadLerp(Vector2[] uvs, int weight1, int weight2, int weight3, int weight4, Vector2 point){
        //First get order of these uvs
        Vector2 a, b, c, d;
        int[] mindices = new int[4];
        Vector2[] uvsSorted = new Vector2[4];

        //We'll start with a shitsort of uvs by y ascending (since uv map top left is y = 0)
        for(int i = 0; i < uvs.Length; i++){
            int numGreater = 0;
            for(int j = 0; j < uvs.Length; j++){
                if(i == j){continue;}
                if(uvs[i].y < uvs[j].y){numGreater++;}
            }
            mindices[i] = numGreater;
        }
        for(int i = 0; i < uvs.Length; i++){
            if(mindices[i] == 3){uvsSorted[0] = uvs[i];}
            else if(mindices[i] == 2){uvsSorted[1] = uvs[i];}
            else if(mindices[i] == 1){uvsSorted[2] = uvs[i];}
            else{uvsSorted[3] = uvs[i];}
        }

        //Now we can order them in clockwise fashion with a being top left
        if(uvsSorted[0].x < uvsSorted[1].x){
            a = uvsSorted[0];
            b = uvsSorted[1];
        }else{
            a = uvsSorted[1];
            b = uvsSorted[0];
        }
        if(uvsSorted[2].x < uvsSorted[3].x){

        }

        //Normalize and lerp
        float xmin = Mathf.Min(a.x, b.x);
        float xmax = Mathf.Max(a.x, b.x);
        float u = (point.x - xmin) / (xmax - xmin);
        Vector3 abu = Vector3.Lerp(a, b, u);

        float xmin = Mathf.Min(a.x, b.x);
        float xmax = Mathf.Max(a.x, b.x);
        float u = (point.x - xmin) / (xmax - xmin);
        Vector3 abu = Vector3.Lerp(a, b, u);
    }*/

    static bool IsZero(int[] array){
        bool r = false;
        foreach(int i in array){
            if(i != 0) r = true;
        }
        return r;
    }

    //Generates a map of pixels of center of quads
    static Vector2[][] GenerateQuadDictDebugMap(Dictionary<int, List<QuadInfo>> quadDict, Vector2[] uvs, float imageSize){
        List<Vector2> returnList1 = new();
        List<Vector2> returnList2 = new();
        for(int i = 0; i < uvs.Length; i++){
            if (quadDict.ContainsKey(i)){
                for(int j = 0; j < quadDict[i].Count; j++){
                    Vector2 center;
                    if(IsZero(quadDict[i][j].tri2)){
                        float centerX = (uvs[quadDict[i][j].tri1[0]].x + uvs[quadDict[i][j].tri1[1]].x + uvs[quadDict[i][j].tri1[2]].x + uvs[quadDict[i][j].tri2[0]].x) / 4;
                        float centerY = (uvs[quadDict[i][j].tri1[0]].y + uvs[quadDict[i][j].tri1[1]].y + uvs[quadDict[i][j].tri1[2]].y + uvs[quadDict[i][j].tri2[0]].y) / 4;
                        center = new Vector2(centerX, centerY);
                        returnList1.Add(UVCoordToPixel(center, imageSize));
                        returnList2.Add(Vector2.zero);
                    }
                    else{
                        center = (uvs[quadDict[i][j].tri1[0]] + uvs[quadDict[i][j].tri1[1]] + uvs[quadDict[i][j].tri1[2]]) / 3;
                        returnList2.Add(UVCoordToPixel(center, imageSize));
                        returnList1.Add(Vector2.zero);
                    }
                }
            }
        }
        return new Vector2[][] {returnList1.ToArray(), returnList2.ToArray()};
    }
}
