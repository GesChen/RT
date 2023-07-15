using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RTObject : MonoBehaviour
{
    [Header("Material")]
    public Color color;
    public Color eColor;
    public float eIntensity;

    Vector3 pos;
    Quaternion rot;
    Vector3 scl;
    void Update()
    {
        Vector3 curPos    = transform.position;
        Quaternion curRot = transform.rotation;
        Vector3 curScl    = transform.localScale;
        // detect if object has changed
        if (pos != curPos || rot != curRot || scl != curScl)
        {
            RTMain.worldChanged.Invoke();
        }
        pos = curPos;
        rot = curRot;
        scl = curScl;
    }
}
