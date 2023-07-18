using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public struct Triangle
{
	public Vector3 v0;
	public Vector3 v1;
	public Vector3 v2;

	public Vector3 color;
	//public Vector3 eColor;
	//public float eIntensity;
}
public struct ObjectBounds
{
	public Vector3 boundsStart;
	public Vector3 boundsEnd;

	public int indexStart;
	public int indexEnd;
}

public class RTMain : MonoBehaviour
{
	[Header("Settings")]
	//[Range(0, 1000)]
	public int Samples;
	//[Range(0, 20)]
	public int MaxBounces;
	public bool denoise = true;
	public float denoiseStrength;

	[Header("Backend")]
	public ComputeShader raytracer;
	public ComputeShader denoiser;
	public ComputeShader TAA;
	public RenderTexture texture;
	public RenderTexture normalsTexture;
	private RenderTexture lastFrame;
	ComputeBuffer worldBuffer;
	ComputeBuffer boundsBuffer;
	Camera camera;

	public static UnityEvent worldChanged;

	List<Triangle> tris;
	List<ObjectBounds> bounds;
	Vector3 ColorToVec3(Color col)
	{
		return new Vector3(col.r, col.g, col.b);
	}
	void Start()
	{
		Application.targetFrameRate = 60; //no unnecessary processing

		camera = Camera.main;

		worldChanged = new UnityEvent();
		worldChanged.AddListener(UpdateWorld);

		if (texture == null)
		{
			texture = new RenderTexture(camera.pixelWidth, camera.pixelHeight, 24)
			{
				enableRandomWrite = true
			};
			texture.Create();
		}
        if (lastFrame == null)
        {
            lastFrame = new RenderTexture(camera.pixelWidth, camera.pixelHeight, 24)
            {
                enableRandomWrite = true
            };
            lastFrame.Create();
        }
        if (normalsTexture == null)
        {
            normalsTexture = new RenderTexture(camera.pixelWidth, camera.pixelHeight, 24)
            {
                enableRandomWrite = true
            };
            normalsTexture.Create();
        }
    }
	void UpdateWorld()
	{
		// populate tris with all tris from meshes of rtobjects
		int i = 0;
		tris = new();
		bounds = new();
		foreach (RTObject obj in FindObjectsOfType<RTObject>())
		{
			// dont render if object isn't in frustum
			if (obj.visibleToCamera)
			{
				Mesh mesh = obj.GetComponent<MeshFilter>().sharedMesh;

				Bounds rendererBounds = obj.GetComponent<Renderer>().bounds;

				bounds.Add(new ObjectBounds
				{
					boundsStart = rendererBounds.min,
					boundsEnd = rendererBounds.max,
					indexStart = i,
					indexEnd = i + mesh.triangles.Length / 3 - 1
				});

				for (int t = 0; t < mesh.triangles.Length / 3; t++) //iterate through tris
				{
					i++;
					tris.Add(new Triangle
					{
						v0 = obj.transform.TransformPoint(mesh.vertices[mesh.triangles[t * 3 + 0]]),
						v1 = obj.transform.TransformPoint(mesh.vertices[mesh.triangles[t * 3 + 1]]),
						v2 = obj.transform.TransformPoint(mesh.vertices[mesh.triangles[t * 3 + 2]]),
						color = ColorToVec3(obj.color)//,
													  //eColor = col2vec3(obj.eColor),
													  //eIntensity = obj.eIntensity,
					});
				}
			}
		}
	}
	private void OnRenderImage(RenderTexture src, RenderTexture dest)
	{
		BeforeMain();

		raytracer.Dispatch(0, texture.width / 16, texture.height / 16, 1);

		AfterMain();

		if (denoise)
		{
			Denoise();
			AntiAliasing();
		}

		// show texture to screen
		Graphics.Blit(texture, dest);
		lastFrame = texture;
	}

	void BeforeMain() // pass values into shader and other setup
	{
		/*
		// debug
		ComputeBuffer debugBuffer = new(1, sizeof(int));
		debugBuffer.SetData(new int[] { 0 });
		shader.SetBuffer(0, "debug", debugBuffer);
		*/

		// world and bounds buffer
		if (tris.Count > 0)
		{
			int worldSize = sizeof(float) * 3 * 4; // 3 for vec3, 4 for 4 vec3s 
			worldBuffer = new(tris.Count, worldSize);
			worldBuffer.SetData(tris.ToArray());
			raytracer.SetBuffer(0, "World", worldBuffer);
			raytracer.SetInt("worldSize", tris.Count);

			int boundsSize = sizeof(float) * 3 * 2 + sizeof(int) * 2;
			boundsBuffer = new(bounds.Count, boundsSize);
			boundsBuffer.SetData(bounds.ToArray());
			raytracer.SetBuffer(0, "WorldObjectBounds", boundsBuffer);
			raytracer.SetInt("numObjects", bounds.Count);
		}
		else
		{
			raytracer.SetInt("worldSize", 0);
			raytracer.SetInt("numObjects", 0);
		}

		// general
		raytracer.SetTexture(0, "Output", texture);
		raytracer.SetTexture(0, "Normals", normalsTexture);
		raytracer.SetFloat("Time", Time.time);
		raytracer.SetInt("samples", Samples);
		raytracer.SetInt("max_bounces", MaxBounces);

		// set camera values
		float planeHeight = camera.nearClipPlane * Mathf.Tan(Mathf.Deg2Rad * camera.fieldOfView * .5f) * 2f;
		float planeWidth = planeHeight * camera.aspect;
		float[] viewParams = new float[3] { planeWidth, planeHeight, camera.nearClipPlane };
		raytracer.SetFloats("viewParams", viewParams); // view parameters 

		Vector3 ctp = camera.transform.position;
		raytracer.SetFloats("camPos", new float[3] { ctp.x, ctp.y, ctp.z }); // set position

		Vector3 ctre = camera.transform.rotation.eulerAngles;
		raytracer.SetFloats("camRot", new float[3] { ctre.x, ctre.y, ctre.z }); // set rotation

		raytracer.SetFloats("res", new float[2] { texture.width, texture.height });
	}
	void AfterMain()
	{
		/* debug pt2
		int[] debugout = new int[1];
		debugBuffer.GetData(debugout);
		Debug.Log("buffer " + debugout[0]);
		*/

		// release memory
		if (tris.Count > 0)
		{
			worldBuffer.Dispose();
			boundsBuffer.Dispose();
		}
	}
	void Denoise()
	{
		denoiser.SetFloat("denoiseStrength", denoiseStrength);
        denoiser.SetInts("resolution", new int[2] { texture.width, texture.height });
		denoiser.SetTexture(0, "Image", texture);
		denoiser.SetTexture(0, "Normals", normalsTexture);

		denoiser.Dispatch(0, texture.width / 16, texture.height / 16, 1);
    }
	void AntiAliasing()
	{
        TAA.SetInts("resolution", new int[2] { texture.width, texture.height });
        TAA.SetTexture(0, "Image", texture);
		TAA.SetTexture(0, "LastFrame", lastFrame);

        TAA.Dispatch(0, texture.width / 16, texture.height / 16, 1);
    }
}