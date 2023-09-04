using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using TMPro;

public class RelayTest : MonoBehaviour
{
    [SerializeField] private GameObject Canvas;
    [SerializeField] private TMP_InputField input;
    [SerializeField] private TMP_Text text;
    string code;

    private async void Start()
    {
        await UnityServices.InitializeAsync();

        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log("Signed in as " + AuthenticationService.Instance.PlayerId);
        };

        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    public async void CreateRelay()
    {
        try
        {
            Allocation a = await RelayService.Instance.CreateAllocationAsync(3);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(a.AllocationId);
            Debug.Log(joinCode);
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetHostRelayData(
                a.RelayServer.IpV4,
                (ushort)a.RelayServer.Port,
                a.AllocationIdBytes,
                a.Key,
                a.ConnectionData
            );

            NetworkManager.Singleton.StartHost();
            code = joinCode;
            Canvas.SetActive(false);
            text.text = joinCode;
        }
        catch (RelayServiceException e)
        {
            Debug.Log(e);
            Canvas.SetActive(true);
        }
    }

    public void Join() => JoinRelay(input.text);
    public void Leave()
    {
        NetworkManager.Singleton.Shutdown();

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        Canvas.SetActive(true);
    }

    private async void JoinRelay(string joinCode)
    {
        try 
        {
            Debug.Log("Attempting join with code " + joinCode);

            JoinAllocation j = await RelayService.Instance.JoinAllocationAsync(joinCode);

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetClientRelayData(
                j.RelayServer.IpV4,
                (ushort)j.RelayServer.Port,
                j.AllocationIdBytes,
                j.Key,
                j.ConnectionData,
                j.HostConnectionData
            );

            NetworkManager.Singleton.StartClient();
            Canvas.SetActive(false);
        }
        catch (RelayServiceException e) 
        {
            Debug.Log(e);
            Canvas.SetActive(true);
        }

    }
}
