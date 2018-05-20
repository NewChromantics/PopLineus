using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;



public struct int2
{
	public int x;
	public int y;

	public int2(int x, int y)
	{
		this.x = x;
		this.y = y;
	}
};

public struct Line2
{
	public int2 Start;
	public int2 End;

	public Line2(int2 Start, int2 End)
	{
		this.Start = Start;
		this.End = End;
	}

	public Line2(int x0, int y0, int x1, int y1)
	{
		this.Start = new int2(x0, y1);
		this.End = new int2(x1, y1);
	}
}



[System.Serializable]
public class UnityEvent_String : UnityEngine.Events.UnityEvent<string> { };

public class LineusSharp : MonoBehaviour {

	const string Hostname = "line-us.local";
	const int Port = 1337;

	const int MinY = 1000;
	const int MaxY = -1000;
	const int MinX = 700;
	const int MaxX = 1800;

	//	protocol stuff
	public static readonly byte[] LineTerminator = new byte[3] { (byte)'\r', (byte)'\n', (byte)'\0' };
	const string Response_Hello = "hello ";
	const string Response_Ok = "ok ";
	const string Response_Error = "error ";


	TcpClient	Socket;
	bool RunThread;

	List<System.Action> MainThreadQueue;
	public UnityEngine.Events.UnityEvent OnConnected;
	public UnityEngine.Events.UnityEvent OnReady;
	public UnityEvent_String OnError;

	string PendingCommand = null;			//	if non-null, we're waiting for an ok/error response from this command
	List<string> CommandQueue;


	void QueueJob(System.Action Job)
	{
		if (MainThreadQueue == null)
			MainThreadQueue = new List<System.Action>();

		lock( MainThreadQueue)
		{
			MainThreadQueue.Add(Job);
		}
	}

	System.Action PopJob()
	{
		if (MainThreadQueue == null)
			return null;

		System.Action Job = null;
		lock (MainThreadQueue)
		{
			if (MainThreadQueue.Count > 0)
			{
				Job = MainThreadQueue[0];
				MainThreadQueue.RemoveAt(0);
			}
		}
		return Job;
	}

	void Update()
	{
		//	lets throttle this a bit...
		for (var i = 0; i < 20; i++)
		{
			var Job = PopJob();
			if (Job == null)
				continue;
			Job.Invoke();
		}
	}

	string PopNextResponse(ref List<byte> Buffer)
	{
		for (int i = 0; i <=Buffer.Count - LineTerminator.Length;	i++ )
		{
			var Match = true;
			for (var t = 0; t < LineTerminator.Length;	t++ )
			{
				if (Buffer[i + t] != LineTerminator[t])
					Match = false;
			}
			if ( Match )
			{
				var LineLength = i;
				var Line = System.Text.ASCIIEncoding.ASCII.GetString(Buffer.ToArray(), 0, LineLength);
				Buffer.RemoveRange( 0, LineLength);
				Buffer.RemoveRange( 0, LineTerminator.Length );
				return Line;
			}
		}
		return null;
	}

	void ProcessResponse(string Response)
	{
		if (Response.StartsWith(Response_Hello))
		{
			QueueJob(() => { OnReady.Invoke(); } );
		}
		else if (Response.StartsWith(Response_Ok))
		{
			PendingCommand = null;
		}
		else if (Response.StartsWith(Response_Error))
		{
			PendingCommand = null;
		}
		else
		{
			Debug.Log("Unknown response " + Response);
		}
	}

	void ProcessBuffer(ref List<byte> Buffer)
	{
		while ( true )
		{
			var Next = PopNextResponse(ref Buffer);
			if (Next == null)
				break;

			ProcessResponse(Next);
		}
	}

	void SocketThread(object x)
	{
		//	gr: do this in a thread!
		Socket = new TcpClient(Hostname, Port);

		if ( Socket.Connected )
		{
			QueueJob(() => { OnConnected.Invoke(); });
		}
		else
		{
			QueueJob( ()=>{OnError.Invoke("Didn't connect");});
			return;
		}

		var Stream = Socket.GetStream();

		var Buffer = new List<byte>();

		while (RunThread)
		{
			//	commands pending!
			if (PendingCommand == null)
			{
				PendingCommand = PopCommand();
				if (PendingCommand != null)
				{
					Debug.Log("New command: " + PendingCommand);
					var PendingCommandBytes = System.Text.ASCIIEncoding.ASCII.GetBytes(PendingCommand);
					Stream.Write(PendingCommandBytes, 0, PendingCommandBytes.Length);
					Stream.Write(LineTerminator, 0, LineTerminator.Length);
				}
			}

			//	read() is blocking
			if (!Stream.DataAvailable)
			{
				System.Threading.Thread.Sleep(500);
			}
			else
			{
				var ReadBuffer = new byte[1024];
				var Read = Stream.Read(ReadBuffer, 0, ReadBuffer.Length);
				if (Read == 0)
				{
					System.Threading.Thread.Sleep(500);
				}

				for (int i = 0; i < Read; i++)
					Buffer.Add(ReadBuffer[i]);

				ProcessBuffer(ref Buffer);
			}
		}

	}

	void Disconnect()
	{
		RunThread = false;
	}

	public void Connect()
	{
		RunThread = true;
		System.Threading.ThreadPool.QueueUserWorkItem(new System.Threading.WaitCallback(SocketThread));
	}

	void OnDisable()
	{
		Disconnect();
	}

	void QueueCommand(string Command)
	{
		if (CommandQueue == null)
			CommandQueue = new List<string>();

		lock (CommandQueue)
		{
			CommandQueue.Add(Command);
		}
	}

	string PopCommand()
	{
		if (CommandQueue == null)
			return null;

		string Command = null;
		lock (CommandQueue)
		{
			if ( CommandQueue.Count > 0 )
			{
				Command = CommandQueue[0];
				CommandQueue.RemoveAt(0);
			}
		}
		return Command;
	}

	void QueueCommand_LiftPen()
	{
		QueueCommand("G01 Z1000");
	}

	void QueueCommand_MoveTo(int x,int y,bool PenDown)
	{
		var z = PenDown ? 0 : 1000;
		QueueCommand("G01 X" + x + " Y" + y + " Z" + z);
	}


	public void Draw(IEnumerable<Line2> Lines)
	{
		foreach (var Line in Lines)
		{
			var x0 = Line.Start.x;
			var y0 = Line.Start.y;
			var x1 = Line.End.x;
			var y1 = Line.End.y;
			QueueCommand_LiftPen();
			QueueCommand_MoveTo(x0, y0, false);
			QueueCommand_MoveTo(x0, y0, true);
			QueueCommand_MoveTo(x1, y1, true);
		}
	}

	public void Draw(IEnumerable<Vector2> Coords)
	{
		var First = true;
		foreach ( var Coord in Coords )
		{
			var x = (int)Mathf.Lerp(MinX, MaxX, Coord.x);
			var y = (int)Mathf.Lerp(MinY, MaxY, Coord.y);

			if ( First )
			{
				QueueCommand_LiftPen();
				QueueCommand_MoveTo(x,y,false);
				First = false;
			}
			QueueCommand_MoveTo(x, y, true);
		}
	}
}
