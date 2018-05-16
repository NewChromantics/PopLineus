using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineusSharp))]
public class DrawSquare : MonoBehaviour {

	public LineusSharp Lineus	{ get { return GetComponent<LineusSharp>(); }}

	void OnEnable()
	{
		Lineus.OnReady.AddListener(SendDrawSquare);
		Lineus.OnError.AddListener(OnError);
		Lineus.Connect();
	}

	void OnDisable()
	{
		Lineus.OnReady.RemoveListener(SendDrawSquare);
		Lineus.OnError.RemoveListener(OnError);
	}

	void SendDrawSquare()
	{
		var Lineus = this.Lineus;

		var Coords = new List<Vector2>();
		Coords.Add(new Vector2(0, 0));
		Coords.Add(new Vector2(1, 0));
		Coords.Add(new Vector2(1, 1));
		Coords.Add(new Vector2(0, 1));
		Coords.Add(new Vector2(0, 0));

		Lineus.Draw(Coords);
	}

	void OnError(string Error)
	{
		Debug.LogError(Error);
	}
}
