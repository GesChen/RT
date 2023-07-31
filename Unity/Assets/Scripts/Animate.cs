using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Animate : MonoBehaviour
{
	public float speed = 1f;
	[Space]
	public bool animateTranslation;
	public bool animateRotation;
	public bool animateScaling;
    [Space]
    public bool loopingTranslate;
	public bool loopingRotation;
	public bool loopingScaling;
    [Space]
    public Vector3 translateAmount;
	public Vector3 rotateAmount;
	public Vector3 scaleAmount;
    [Space]
    public float translatePeriod;
	public float rotatePeriod;
	public float scalePeriod;
    [Space] 
    public Vector3 initTranslation;
    public Vector3 initRotation;
    public Vector3 initScale;

    private Vector3 translation;
	private Vector3 rotation;
	private Vector3 scale;
	
	void Start()
	{
		initTranslation = transform.localPosition;
		initRotation = transform.localRotation.eulerAngles;
		initScale = transform.localScale;
	}
	void Update()
	{
		float np = Time.time;
		float lp = Mathf.Sin(np * speed);

		if(animateTranslation)
			translation = loopingTranslate ? lp * translateAmount : np * translateAmount;
		if(animateRotation)
			rotation = loopingRotation ? lp * rotateAmount : np * rotateAmount;
		if(animateScaling)
			scale = loopingScaling ? lp * scaleAmount : np * scaleAmount;

		transform.localPosition = initTranslation + translation;
		transform.localRotation = Quaternion.Euler(initRotation + rotation);
		transform.localScale = initScale + scale;
	}
}
