using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{
	public float sensitivity = 60f;
	public float minSpeed = .1f;
	public float maxSpeed = 10f;
    public float speed;
    public float drift = .2f;
	public float scrollSensitivity = 5f;
	public float angleLimit = 85f;

	Vector3 velocity;
	float pitch;
	float yaw;

	void Start()
	{
		speed = maxSpeed;
		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
	}

	void Update()
	{
		pitch -= Input.GetAxis("Mouse Y") * sensitivity * Time.deltaTime;
		pitch = Mathf.Clamp(pitch, -angleLimit, angleLimit);
		yaw   += Input.GetAxis("Mouse X") * sensitivity * Time.deltaTime;

		transform.rotation = Quaternion.Euler(pitch, yaw, 0);

		float updown;
        if (Input.GetKey("e"))
            updown = 1;
        else if(Input.GetKey("q"))
            updown = -1;
		else
			updown = 0;

        velocity = Vector3.Lerp(velocity, 
			Input.GetAxisRaw("Vertical") * speed * transform.forward + 
			Input.GetAxisRaw("Horizontal") * speed * transform.right + 
			speed * updown * transform.up, drift);

		transform.position += velocity * Time.deltaTime;

		speed = Mathf.Clamp(speed + Input.mouseScrollDelta.y * scrollSensitivity * Time.deltaTime, minSpeed, maxSpeed);
	}
}
