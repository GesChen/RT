using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RTObject : MonoBehaviour
{
    [Header("Material")]
    public Color color;
    [Range(0, 1)]
    public float smoothness;
    public float emission;
    [Space]
    public bool visibleToCamera;

    Vector3 pos;
    Quaternion rot;
    Vector3 scl;
    bool lastVisible;
    void Update()
    {
        Vector3 curPos    = transform.position;
        Quaternion curRot = transform.rotation;
        Vector3 curScl    = transform.localScale;

        /* raycast method (needs improvement)
        Transform cam = Camera.main.transform;
        RaycastHit hit;
        Physics.Raycast(cam.position, (transform.position - cam.position).normalized, out hit);
        visibleToCamera = hit.transform == transform;
        */
        Plane[] camFrustum = GeometryUtility.CalculateFrustumPlanes(Camera.main);
        visibleToCamera = GeometryUtility.TestPlanesAABB(camFrustum, GetComponent<Renderer>().bounds);

        // detect if object has changed
        if (pos != curPos || rot != curRot || scl != curScl || visibleToCamera != lastVisible)
        {
            RTMain.worldChanged.Invoke();
        }
        pos = curPos;
        rot = curRot;
        scl = curScl;
        lastVisible = visibleToCamera;

    }
}
