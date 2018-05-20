using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class UnityEvent_Texture : UnityEngine.Events.UnityEvent<Texture>
{ }



[ExecuteInEditMode]
public class FindErodedImage : MonoBehaviour {

	public Texture InputImage;
	public Material EdgeFilter;
	public Material LargestFilter;
	public UnityEvent_Texture OnChanged;

	//public int OutputImageWidth { get { return InputImage.width; } }
	//public int OutputImageHeight { get { return InputImage.height; } }
	public int OutputImageWidth = 1024;
	public int OutputImageHeight = 256;

	RenderTexture OutputEdgeImage;
	RenderTexture OutputLargestImage;

	void Update()
	{
		var OutputImage = ProcessImage();
		OnChanged.Invoke(OutputImage);
	}

	Texture ProcessImage () 
	{
		if (OutputEdgeImage == null)
		{
			OutputEdgeImage = new RenderTexture(OutputImageWidth, OutputImageHeight, 0, RenderTextureFormat.ARGBFloat);
			OutputEdgeImage.filterMode = FilterMode.Point;
		}
		if (OutputLargestImage == null)
		{
			OutputLargestImage = new RenderTexture(OutputImageWidth, OutputImageHeight, 0, RenderTextureFormat.ARGBFloat);
			OutputLargestImage.filterMode = FilterMode.Point;
		}
		Graphics.Blit(InputImage, OutputEdgeImage, EdgeFilter);
		Graphics.Blit(OutputEdgeImage, OutputLargestImage, LargestFilter);

		return OutputLargestImage;
	}


	void RemoveLines_Horz(ref bool[,] Mask,System.Action<int2,int2> EnumLine)
	{
		int MinLineWidth = 4;

		for (int y = 0; y < Mask.GetLength(1);	y++)
		{
			int x = 0;
			if (!Mask[x, y])
				continue;

			//	how many in a row?
			int Lastx;
			for (Lastx = x; x < Mask.GetLength(0);	x++ )
			{
				if (!Mask[Lastx, y])
					break;
			}
			Lastx--;
			var Length = Lastx - x;

			if (Length < MinLineWidth)
			{
				x = Lastx;
				continue;
			}

			for (int xx = x; xx <= Lastx; xx++)
				Mask[xx, y] = false;
			EnumLine(new int2(x, y), new int2(Lastx, y));
		}
	}

	public List<Line2> GetLines()
	{
		var OutputImage = ProcessImage();
		var OutputImage2D = GetTexture2D(OutputImage);

		//	turn image into 1/0
		var ImageMask = new bool[OutputImage2D.width, OutputImage2D.height];
		for (int y = 0; y < OutputImage2D.height;	y++ )
		{
			for (int x = 0; x < OutputImage2D.width; x++)
			{
				var Colour = OutputImage2D.GetPixel(x,y);
				ImageMask[x,y] = (Colour.x < 0.5f);
			}
		}

		var Lines = new List<Line2>();

		//	enum & strip lines
		System.Action<int2,int2> EnumLine = (Start,End)=>
		{
			Lines.Add( new Line2(Start,End));
		};
		RemoveLines_Horz(ref ImageMask, EnumLine);

		return Lines;
	}
}
