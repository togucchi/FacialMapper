using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;
using System;
using System.Threading;

public class RightEyeBall_iFacialMocap : MonoBehaviour
{
    string messageString = "";
    UnityRecieve_iFacialMocap _iFacialMocap;

    // Start is called 
    void Start()
    {
        _iFacialMocap = FindObjectOfType<UnityRecieve_iFacialMocap>();
    }

    // Update is called once per frame
    void Update()
    {
        this.messageString = _iFacialMocap.GetMessageString();

        try
		{
			var strArray1 = this.messageString.Split(new Char[] { '=' });
			if(strArray1.Length == 2)
			{
				foreach (string message in strArray1[1].ToString().Split(new Char[] { '|' }))
				{
					var strArray2 = message.Split(new Char[] { '#' });

					if (strArray2.Length == 2)
					{
						if(strArray2[0]=="rightEye")
						{
							var rotationList = strArray2[1].Split(new Char[] { ',' });
							this.transform.localRotation = Quaternion.Euler(float.Parse(rotationList[0]), float.Parse(rotationList[1]), float.Parse(rotationList[2]));
						}
					}
				}
			}
		}
		catch
		{
		}
	}

	void Stop()
	{
	}
}