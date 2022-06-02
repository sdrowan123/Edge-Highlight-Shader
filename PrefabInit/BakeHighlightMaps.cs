using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

//TODO: Debug Distances cuz they're straight up wronggg :()

public class BakeHighlightMaps
{   

    //TODO: Instead of just triangles, find quads each vertex is a part of. Map this before recursive steps.
    //I changed the way it averages so look at that maybe.
    public struct NeighborInfo{
        public int vertexIndex;
        public Vector3[] neighborNormals;
        public Vector3[] neighborPositions;
        public int[] neighborIndices;

        public int[][] trisIndices;
    }

    public static Vector2[] Bake(Mesh mesh, PrefabInitParameters parameters){
        Vector3[] vertices = mesh.vertices;
        Vector3[] normals = mesh.normals;
        Vector4[] tangents = mesh.tangents;
        if(tangents == null){Debug.LogError("Prefab Initialization Mesh Has No Tangents");}
        int[] triangles = mesh.triangles;
        //Sharpness array corresponding to steepness of each vertex between 0 and 1
        float[] sharpness = new float[normals.Length];
        Vector2[] returnSharpness = new Vector2[normals.Length];

        //Generate List of neighbor Info
        NeighborInfo[] info = new NeighborInfo[vertices.Length];
        for(int i = 0; i < vertices.Length; i++){
            info[i] = GetNeighbors(i, vertices, normals, triangles);
        }

        //Debug:
        if(normals.Length != mesh.vertices.Length){Debug.LogError("Prefab normals do not correspond to vertices.");}


        //Calculate Distance:
        float[] distances = GenerateDistances(vertices, info);
        StatInfo distanceStats = CalcStatInfo(distances);

        //float [] distancesMeanified = MeanifyDistances(distances, distanceStats.mean);
        //distanceStats = CalcStatInfo(distancesMeanified);

        float statCoeff = distanceStats.stDev * distanceStats.stDev;
        if (statCoeff < 1){statCoeff = 1 / statCoeff;}
        float[] distancesNormalized = NormalizeSet(distances, distanceStats.mean - distanceStats.stDev, distanceStats.mean + statCoeff);
        distanceStats = CalcStatInfo(distancesNormalized);
        distancesNormalized = MoveMean(distancesNormalized, distanceStats.mean, 0.5f);


        //Calculate Sharpness
        for(int i = 0; i < normals.Length; i++){
            sharpness[i] = CalcSharpness(i, vertices, normals, info);
        }
        StatInfo sharpnessStatInfo = CalcStatInfo(sharpness);
        float[] sharpnessNormalized = NormalizeSet(sharpness, 0, sharpnessStatInfo.max);
        sharpnessStatInfo = CalcStatInfo(sharpnessNormalized);
        sharpnessNormalized = MoveMean(sharpnessNormalized, sharpnessStatInfo.mean, 0.5f);


        //Combine:
        float[] finalSharpness = new float[sharpnessNormalized.Length];
        for(int i = 0; i < sharpnessNormalized.Length; i++){
            finalSharpness[i] = (distancesNormalized[i] + parameters.sharpnessToDistanceRatio * sharpnessNormalized[i]);
        }
        StatInfo finalStats = CalcStatInfo(finalSharpness);
        //Normalization and Clipping stuff
        finalSharpness = MoveMean(finalSharpness, finalStats.mean, 0.3f);
        //float[] finalSharpnessNormalized = NormalizeSet(finalSharpness, finalStats.min, finalStats.max);
        //StatInfo finalStats = CalcStatInfo(finalSharpness);
        //float[] finalSharpnessStandardized = StandardizeSet(finalSharpness, finalStats.mean, finalStats.stDev);

        for(int i = 0; i < sharpnessNormalized.Length; i++){
            returnSharpness[i] = new Vector2(finalSharpness[i], 0);
        }

        return returnSharpness;
    }
    
    static float[] MoveMean(float[] set, float mean, float desiredMean){
        float[] returnSet = set;
        for(int i = 0; i < set.Length; i++){
            //if(finalSharpness[i] > max){finalSharpness[i] = max;}

            returnSet[i] *= desiredMean / mean;
            if(returnSet[i] > 1){returnSet[i] = 1;}
        }
        return returnSet;
    }

    static float Standardize(float oldVal, float mean, float stdev){
        return Mathf.Abs((oldVal - mean) / stdev);
    }

    static float Normalize(float oldVal, float min, float max){
        return((oldVal - min) / (max - min));
    }

    static float[] StandardizeSet(float[] dataSet, float mean, float stdev){
        float[] returnArray = new float[dataSet.Length];
        for(int i = 0; i < dataSet.Length; i++){
            returnArray[i] = Standardize(dataSet[i], mean, stdev);
        }
        return returnArray;
    }

    static float[] NormalizeSet(float[] dataSet, float min, float max){
        float[] returnArray = new float[dataSet.Length];
        for(int i = 0; i < dataSet.Length; i++){
            returnArray[i] = Normalize(dataSet[i], min, max);
        }
        return returnArray;
    }

    struct StatInfo{
        public float mean;
        public float variance;
        public float stDev;
        public float max;

        public float min;
    }

    static StatInfo CalcStatInfo(float[] dataSet){
        //Mean, Min, and Max:
        float mean = 0;
        float min = 999999999999999999;
        float max = 0;

        for(int i = 0; i < dataSet.Length; i++){
            mean += dataSet[i];
            if(dataSet[i] < min){min = dataSet[i];}
            if(dataSet[i] > max){max = dataSet[i];}
        }
        mean = mean / dataSet.Length;
        
        float variance = 0;

        for(int i = 0; i < dataSet.Length; i++){
            variance += Mathf.Pow((dataSet[i] - mean), 2.0f);
        }
        variance = variance / (dataSet.Length - 1);
        float stdev = Mathf.Sqrt(variance);

        return new StatInfo{
            mean = mean,
            variance = variance,
            stDev = stdev,
            max = max,
            min = min
        };
    }

    static NeighborInfo GetNeighbors(int vertexIndex, Vector3[] vertices, Vector3[] normals, int[] triangles){
        List<Vector3> neighborNormals = new();
        List<Vector3> neighborPos = new();
        List<int> neighborIndices = new();
        List<int[]> trisIndices = new();

        static int SecondPass(int index, int[] indices, int[] triangles, Vector3[] vertices){
            //Check that indices are two hypoteneuse vertices
            float BC = Vector3.Distance(vertices[triangles[indices[0]]], vertices[triangles[indices[1]]]);
            float AB = Vector3.Distance(vertices[triangles[index]], vertices[triangles[indices[0]]]);
            float AC = Vector3.Distance(vertices[triangles[index]], vertices[triangles[indices[1]]]);
            if(BC > AB && BC > AC){
                for(int i = 0; i < triangles.Length; i+=3){
                    if(triangles[i] != triangles[index] && ((triangles[i + 1] == triangles[indices[0]] && triangles[i + 2] == triangles[indices[1]]) || (triangles[i + 2] == triangles[indices[0]] && triangles[i + 1] == triangles[indices[1]]))){
                        return triangles[i];
                    }
                    if(triangles[i + 1] != triangles[index] && ((triangles[i] == triangles[indices[0]] && triangles[i + 2] == triangles[indices[1]]) || (triangles[i + 2] == triangles[indices[0]] && triangles[i] == triangles[indices[1]]))){
                        return triangles[i + 1];
                    }
                    if(triangles[i + 2] != triangles[index] && ((triangles[i] == triangles[indices[0]] && triangles[i + 1] == triangles[indices[1]]) || (triangles[i + 1] == triangles[indices[0]] && triangles[i] == triangles[indices[1]]))){
                        return triangles[i + 2];
                    }
                }
            }
            return triangles[index];
        }

        //First Pass
        for(int triangle = 0; triangle < triangles.Length; triangle+=3){
                //Add neighbors
                if(triangles[triangle] == vertexIndex){
                    int farQuadIndex = SecondPass(triangle, new int[] {triangle + 1, triangle + 2}, triangles, vertices);
                    neighborNormals.Add(normals[triangles[triangle + 1]]);
                    neighborNormals.Add(normals[triangles[triangle + 2]]);
                    if(farQuadIndex != triangles[triangle]){neighborNormals.Add(normals[farQuadIndex]);}

                    neighborPos.Add(vertices[triangles[triangle + 1]]);
                    neighborPos.Add(vertices[triangles[triangle + 2]]);
                    if(farQuadIndex != triangles[triangle]){neighborPos.Add(vertices[farQuadIndex]);}

                    neighborIndices.Add(triangles[triangle + 1]);
                    neighborIndices.Add(triangles[triangle + 2]);
                   if(farQuadIndex != triangles[triangle]){ neighborIndices.Add(farQuadIndex);}
                   //else{Debug.LogError("FFFFFFF1");}

                   trisIndices.Add(new int[] {triangles[triangle], triangles[triangle + 1], triangles[triangle + 2]});

                }else if(triangles[triangle + 1] == vertexIndex){
                    int farQuadIndex = SecondPass(triangle + 1, new int[] {triangle, triangle + 2}, triangles, vertices);
                    neighborNormals.Add(normals[triangles[triangle]]);
                    neighborNormals.Add(normals[triangles[triangle + 2]]);
                    if(farQuadIndex != triangles[triangle + 1]){neighborNormals.Add(normals[farQuadIndex]);}

                    neighborPos.Add(vertices[triangles[triangle]]);
                    neighborPos.Add(vertices[triangles[triangle + 2]]);
                    if(farQuadIndex != triangles[triangle + 1]){neighborPos.Add(vertices[farQuadIndex]);}

                    neighborIndices.Add(triangles[triangle]);
                    neighborIndices.Add(triangles[triangle + 2]);
                    if(farQuadIndex != triangles[triangle + 1]){neighborIndices.Add(farQuadIndex);}
                    //else{Debug.LogError("FFFFFFF2");}

                    trisIndices.Add(new int[] {triangles[triangle], triangles[triangle + 1], triangles[triangle + 2]});

                }else if(triangles[triangle + 2] == vertexIndex){
                    int farQuadIndex = SecondPass(triangle + 2, new int[] {triangle, triangle + 1}, triangles, vertices);
                    neighborNormals.Add(normals[triangles[triangle]]);
                    neighborNormals.Add(normals[triangles[triangle + 1]]);
                    if(farQuadIndex != triangles[triangle + 2]){neighborNormals.Add(normals[farQuadIndex]);}

                    neighborPos.Add(vertices[triangles[triangle]]);
                    neighborPos.Add(vertices[triangles[triangle + 1]]);
                    if(farQuadIndex != triangles[triangle + 2]){neighborPos.Add(vertices[farQuadIndex]);}

                    neighborIndices.Add(triangles[triangle]);
                    neighborIndices.Add(triangles[triangle + 1]);
                    if(farQuadIndex != triangles[triangle + 2]){neighborIndices.Add(farQuadIndex);}
                    //else{Debug.LogError("FFFFFFF3");}

                    trisIndices.Add(new int[] {triangles[triangle], triangles[triangle + 1], triangles[triangle + 2]});
                }
            }
        
        if(neighborNormals.Count > 1){
            return new NeighborInfo{vertexIndex = vertexIndex, neighborNormals = neighborNormals.ToArray(), neighborPositions = neighborPos.ToArray(), neighborIndices = neighborIndices.ToArray(), trisIndices = trisIndices.ToArray()};
        }else{
            return new NeighborInfo{vertexIndex = -1, neighborNormals = neighborNormals.ToArray(), neighborPositions = neighborPos.ToArray(), neighborIndices = neighborIndices.ToArray(), trisIndices = trisIndices.ToArray()};
        }
    }

    static float[] GenerateDistances(Vector3[] vertices, NeighborInfo[] infos){
        float[] returnDistances = new float[vertices.Length];
        for(int i = 0; i < vertices.Length; i++){
            NeighborInfo info = infos[i];

            float currDistance = 0;
            float numDistances = 0;
            foreach (Vector3 neighborPos in info.neighborPositions){
                currDistance += Vector3.Distance(neighborPos, vertices[i]);
                numDistances += 1;
            }

            float averageDistance = currDistance / numDistances;

            int depth = 3;
            int coeff = 1;
            float deltaCoeff = 2f;
            float[] neighborDistances = GenerateDistancesRecursive(i, vertices, i, depth, coeff, deltaCoeff, new List<int>(), infos);

            float neighborAverage = neighborDistances[0] / neighborDistances[1];

            returnDistances[i] = neighborAverage / averageDistance;
        }
        return returnDistances;
    }

    static float[] GenerateDistancesRecursive(int index, Vector3[] vertices, int originalIndex, int depthToGo, float coeff, float deltaCoeff, List<int> visitedIndices, NeighborInfo[] infos){
        visitedIndices.Add(index);

        NeighborInfo info = infos[index];
        float currDistance = 0;
        float numDistances = 0;
        if(index != originalIndex){
            foreach (Vector3 neighborPos in info.neighborPositions){
                currDistance += Vector3.Distance(neighborPos, vertices[index]);
                numDistances += 1;
            }
        }
        
        if(depthToGo == 0){return new float[] {coeff * currDistance, coeff * numDistances};}
        else{
            //Recursive Step
            float[] returnFloat = new float[] {coeff * currDistance, coeff * numDistances};
            
            for(int i = 0; i < info.neighborNormals.Length; i++){
                if(!visitedIndices.Contains(info.neighborIndices[i])){
                    float[] returnFloat2 = GenerateDistancesRecursive(info.neighborIndices[i], vertices, originalIndex, depthToGo - 1, coeff * deltaCoeff, deltaCoeff, visitedIndices, infos);
                    returnFloat[0] += returnFloat2[0];
                    returnFloat[1] += returnFloat2[1];
                }
            }
            return returnFloat;
        }
    }

    static float[] MeanifyDistances(float[] distances, float meanDistance){
        float[] returnDistances = new float[distances.Length];
        for(int i = 0; i < distances.Length; i++){
            returnDistances[i] = meanDistance / distances[i];
        }
        return returnDistances;
    }

    static float CalcSharpness(int index, Vector3[] vertices, Vector3[] normals, NeighborInfo[] infos){
        //Overall Strategy:
        //Average sharpness on connected vertices via triangles. Continue this out to average smoothing with lowering coefficients the farther it gets.

        //Debug:
        //Calculate Average Triangle Normal Vector
        //Vector to get angle around should be perpindicular to that average
        /*
        Vector3 averageNormal = normal;
        foreach(Vector3 neighborNormal in neighborNormals){
            averageNormal += neighborNormal;
        }
        averageNormal = averageNormal.normalized;
        Vector3 axis = Vector3.Cross((averageNormal - normal), Vector3.up).normalized;
        */
        int depth = 2;
        float startCoeff = 1;
        float deltaCoeff = 0.5f;
        List<int> visitedIndices = new();
        float[] sharpnessValues = CalcSharpnessRecursive(index, vertices, normals, index, depth, startCoeff, deltaCoeff, visitedIndices, infos);
        return sharpnessValues[0] / sharpnessValues[1];
    }

    static float[] CalcSharpnessRecursive(int index, Vector3[] vertices, Vector3[] normals, int originalIndex, int depthToGo, float coeff, float deltaCoeff, List<int> visitedIndices, NeighborInfo[] infos){
        visitedIndices.Add(index);
        NeighborInfo neighborInfo = infos[index];
        if (neighborInfo.vertexIndex == -1) {return new float[] {0, 0};}

        float anglePerDistTotal = 0;
        float num = 0;
        for(int i = 0; i < neighborInfo.neighborNormals.Length; i++){
            if(!visitedIndices.Contains(neighborInfo.neighborIndices[i])){
                //Calculate Rotation Axis
                Vector3 side1 = Vector3.Normalize(vertices[neighborInfo.vertexIndex] - vertices[originalIndex]);
                Vector3 side2 = normals[originalIndex];
                Vector3 axis = Vector3.Normalize(Vector3.Cross(side2, side1));

                float angle = Mathf.Abs(Vector3.SignedAngle(normals[originalIndex], neighborInfo.neighborNormals[i], axis));
                if (angle == 0){angle = 0.1f;}

                anglePerDistTotal += (angle * neighborInfo.neighborNormals[i].magnitude);
                num += 1;
            }
        }
        
        if(depthToGo == 0){return new float[] {coeff * anglePerDistTotal, coeff * num};}
        else{
            //Recursive Step
            float[] returnFloat = new float[] {coeff * anglePerDistTotal, coeff * num};
            
            for(int i = 0; i < neighborInfo.neighborNormals.Length; i++){
                if(!visitedIndices.Contains(neighborInfo.neighborIndices[i])){
                    float[] returnFloat2 = CalcSharpnessRecursive(neighborInfo.neighborIndices[i], vertices, normals, originalIndex, depthToGo - 1, coeff * deltaCoeff, deltaCoeff, visitedIndices, infos);
                    returnFloat[0] += returnFloat2[0];
                    returnFloat[1] += returnFloat2[1];
                }
            }
            return returnFloat;
        }
    }
}
