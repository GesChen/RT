using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Triangle
{
	public Vector3 v0;
	public Vector3 v1;
	public Vector3 v2;

	public Vector3 color;
	//public Vector3 eColor;
	//public float eIntensity;
}

public class RTMain : MonoBehaviour
{
	public ComputeShader shader;
	public RenderTexture texture;
    ComputeBuffer worldBuffer;
    Camera camera;
	
	readonly List<Triangle> tris = new();
	Vector3 ColorToVec3(Color col)
	{
		return new Vector3(col.r, col.g, col.b);
	}
	void Start()
	{
		Application.targetFrameRate = 60; //no unnecessary processing

		// populate tris with all tris from meshes of rtobjects
		foreach(RTObject obj in FindObjectsOfType<RTObject>())
		{
			Mesh mesh = obj.GetComponent<MeshFilter>().sharedMesh;

			Debug.Log("Adding " + obj.name);
			for (int t = 0; t < mesh.triangles.Length / 3; t++) //iterate through tris
			{
				Debug.Log("Tri " + t);
				tris.Add(new Triangle
				{
					v0 = mesh.vertices[mesh.triangles[t * 3 + 0]] + obj.transform.position,
					v1 = mesh.vertices[mesh.triangles[t * 3 + 1]] + obj.transform.position,
					v2 = mesh.vertices[mesh.triangles[t * 3 + 2]] + obj.transform.position,
					color = ColorToVec3(obj.color)//,
					//eColor = col2vec3(obj.eColor),
					//eIntensity = obj.eIntensity,
				});
			}
		}
	}
	private void OnRenderImage(RenderTexture src, RenderTexture dest)
	{
		//initialize
		camera = GetComponent<Camera>();
		if (texture == null)
		{
			texture = new RenderTexture(camera.pixelWidth, camera.pixelHeight, 24){
				enableRandomWrite = true };
			texture.Create();
		}

		// pass values into shader and other setup
		BeforeDispatch();

		// run shader
        shader.Dispatch(0, texture.width / 8, texture.height / 8, 1);

		// clean up 
		AfterDispatch();

        // show texture to screen
        Graphics.Blit(texture, dest); 
	}

    void BeforeDispatch()
	{
        /*
		// debug
		ComputeBuffer debugBuffer = new(1, sizeof(int));
		debugBuffer.SetData(new int[] { 0 });
		shader.SetBuffer(0, "debug", debugBuffer);
		*/

        // world buffer
        int size = sizeof(float) * 3 * 4; // 3 for vec3, 4 for 4 vec3s 
		worldBuffer = new(tris.Count, size);
        worldBuffer.SetData(tris.ToArray());
        Debug.Log(worldBuffer.count);
        shader.SetBuffer(0, "World", worldBuffer);
        shader.SetInt("worldSize", tris.Count);

        // general
        shader.SetTexture(0, "Output", texture);
        shader.SetFloat("Time", Time.time);

        // set camera values
        float planeHeight = camera.nearClipPlane * Mathf.Tan(Mathf.Deg2Rad * camera.fieldOfView * .5f) * 2f;
        float planeWidth = planeHeight * camera.aspect;
        float[] viewParams = new float[3] { planeWidth, planeHeight, camera.nearClipPlane };
        shader.SetFloats("viewParams", viewParams); // view parameters 

        Vector3 ctp = camera.transform.position;
        shader.SetFloats("camPos", new float[3] { ctp.x, ctp.y, ctp.z }); // set position

        Vector3 ctre = camera.transform.rotation.eulerAngles;
        shader.SetFloats("camRot", new float[3] { ctre.x, ctre.y, ctre.z }); // set rotation

        shader.SetFloats("res", new float[2] { texture.width, texture.height });


    }
	void AfterDispatch()
	{
		/* debug pt2
		int[] debugout = new int[1];
		debugBuffer.GetData(debugout);
		Debug.Log("buffer " + debugout[0]);
		*/
        worldBuffer.Dispose(); // release memory
    }
}