using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrefabInitParameters : MonoBehaviour
{
    public AnimationCurve distanceCurve;
    public AnimationCurve angleCurve;
    public float intensity;

    public float desiredMean = 0.3f;

    public float sharpnessToDistanceRatio = 0.06f;

    public Material highlightMat;

    public int textureSize = 1024;

    public float UVExpectedVertsDistance = 2f;
}
