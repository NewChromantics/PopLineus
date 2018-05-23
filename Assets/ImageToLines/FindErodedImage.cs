using System.Collections;
using System.Collections.Generic;
using UnityEngine;



[ExecuteInEditMode]
public class FindErodedImage : MonoBehaviour {

	[Range(4,100)]
	public int MinLineWidth = 30;
	public int MinLineHeight{ get { return MinLineWidth; }}

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

	void RemoveLines_Vert(ref bool[,] Mask, System.Action<int2, int2> EnumLine)
	{
		var Height = Mask.GetLength(1);
		var Width = Mask.GetLength(0);

		for (int x = 0;x < Width; x++)
		{
			for (int y = 0; y < Height; y++)
			{
				if (!Mask[x, y])
					continue;

				//	how many in a row?
				int Lasty;
				for (Lasty = y; Lasty < Height; Lasty++)
				{
					if (!Mask[x, Lasty])
						break;
				}
				Lasty--;
				var Length = Lasty - y;

				if (Length < MinLineHeight)
				{
					y = Lasty;
					continue;
				}

				for (int yy = y; yy <= Lasty; yy++)
					Mask[x, yy] = false;
				EnumLine(new int2(x, y), new int2(x, Lasty));
				y = Lasty;
			}
		}
	}

	void RemoveLines_Horz(ref bool[,] Mask,System.Action<int2,int2> EnumLine)
	{
		var Height = Mask.GetLength(1);
		var Width = Mask.GetLength(0);

		for (int y = 0; y < Height;	y++)
		{
			for (int x = 0; x < Width; x++)
			{
				if (!Mask[x, y])
					continue;

				//	how many in a row?
				int Lastx;
				for (Lastx = x; Lastx < Width; Lastx++)
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
				x = Lastx;
			}
		}
	}

	public List<Line2> GetLines()
	{
		var OutputImage = ProcessImage();
		var OutputImage2D = PopX.Textures.GetTexture2D(OutputImage,false);

		//	turn image into 1/0
		var ImageMask = new bool[OutputImage2D.width, OutputImage2D.height];
		for (int y = 0; y < OutputImage2D.height;	y++ )
		{
			for (int x = 0; x < OutputImage2D.width; x++)
			{
				var Colour = OutputImage2D.GetPixel(x,y);
				var Black = (Colour.r < 0.5f);

				if ( Black )
					ImageMask[x, y] = true;
				else
					ImageMask[x, y] = false;
			}
		}

		var Lines = new List<Line2>();

		//	enum & strip lines
		System.Action<int2,int2> EnumLine = (Start,End)=>
		{
			Lines.Add( new Line2(Start,End));
		};
		RemoveLines_Horz(ref ImageMask, EnumLine);
		RemoveLines_Vert(ref ImageMask, EnumLine);

		return Lines;
	}
}
