using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;
using System;
using System.Threading;

public class UnityRecieve_iFacialMocap : MonoBehaviour
{
	static UdpClient udp;
	SkinnedMeshRenderer meshTarget;
	Thread thread;

	private string messageString = "";

	// Start is called 
	void Start()
	{
		int LOCAL_PORT = 50003;
		udp = new UdpClient(LOCAL_PORT);
		udp.Client.ReceiveTimeout = 500;
		this.meshTarget = this.GetComponent<SkinnedMeshRenderer>();

		thread = new Thread(new ThreadStart(ThreadMethod));
		thread.Start();
	}

	// Update is called once per frame
	void Update()
	{
		try
		{
			var strArray1 = this.messageString.Split(new Char[] { '=' });
			if(strArray1.Length == 2)
			{
				foreach (string message in strArray1[0].ToString().Split(new Char[] { '|' }))
				{
					var strArray2 = message.Split(new Char[] { '-' });

					if (strArray2.Length == 2)
					{
						var mappedShapeName = strArray2.GetValue(0).ToString();
						var weight = float.Parse(strArray2.GetValue(1).ToString());

						var index = this.meshTarget.sharedMesh.GetBlendShapeIndex(mappedShapeName);
						if (index > -1)
						{
							this.meshTarget.SetBlendShapeWeight(index, weight);
						}
					}
				}
			}
		}
		catch
		{
		}
	}

	void ThreadMethod()
	{
		while (true)
		{
			try
			{
				IPEndPoint remoteEP = null;
				byte[] data = udp.Receive(ref remoteEP);
				this.messageString = Encoding.ASCII.GetString(data);
			}
			catch
			{
			}
		}
	}
	
	public string GetMessageString()
	{
		return this.messageString;
	}

	void OnApplicationQuit()
	{
		thread.Abort();
	}

	void Stop()
	{
		try
		{
			udp.Close();
			thread.Abort();
		}
		catch (IOException)
		{
		}
	}
}