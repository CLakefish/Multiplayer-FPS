using System.Collections;
using System.Collections.Generic;
using UnityEngine; using Unity.Netcode;
using UnityEngine.UI;
using Unity.Netcode.Transports.UTP;

public class NetworkButtons : MonoBehaviour
{
    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 300));

        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            if (GUILayout.Button("Start Host")) NetworkManager.Singleton.StartHost();
            if (GUILayout.Button("Start Client")) NetworkManager.Singleton.StartClient();
            if (GUILayout.Button("Start Server")) NetworkManager.Singleton.StartServer();
        }

        GUILayout.EndArea();
    }
}
