using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineusSharp))]
public class DrawPicture : MonoBehaviour {

	public LineusSharp Lineus	{ get { return GetComponent<LineusSharp>(); }}
	public FindErodedImage Image;

	void OnEnable()
	{
		Lineus.OnReady.AddListener(ProcessImage);
		Lineus.OnError.AddListener(OnError);
		Lineus.Connect();
	}

	void OnDisable()
	{
		Lineus.OnReady.RemoveListener(ProcessImage);
		Lineus.OnError.RemoveListener(OnError);
	}

	void ProcessImage()
	{
		Debug.Log("OnReady");
		var Lineus = this.Lineus;

		var Lines = Image.GetLines();
		Debug.Log("Drawing " + Lines.Count + " lines");
		Lineus.Draw(Lines);
	}

	void OnError(string Error)
	{
		Debug.LogError(Error);
	}
}
