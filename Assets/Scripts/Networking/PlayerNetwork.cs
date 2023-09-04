using System.Collections;
using System.Collections.Generic;
using UnityEngine; using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PlayerNetwork : NetworkBehaviour
{
    [Header("Server Variables")]
    [SerializeField] private bool serverAuth;
    [SerializeField] private GameObject[] spawnPositions;
    private NetworkVariable<Vector3> networkPosition;
    public NetworkVariable<int> healthPoints = new(10);
    public NetworkVariable<bool> roundStart = new(false);

    [Header("Spawning References")]
    public NetworkVariable<bool> requireSpawn = new(false);
    public NetworkVariable<Vector3> spawnPosition = new(new Vector3(0, 0, 0));

    [Header("References")]
    public static PlayerNetwork Singleton;
    private Vector3 currentVelocity;

    private void Awake()
    {
        var permission = serverAuth ? NetworkVariableWritePermission.Server : NetworkVariableWritePermission.Owner;
        networkPosition = new(writePerm: permission);

        spawnPositions = GameObject.FindGameObjectsWithTag("Spawn");
    }

    void Update()
    {
        if (IsOwner) networkPosition.Value = transform.position;
        else transform.position = Vector3.SmoothDamp(transform.position, networkPosition.Value, ref currentVelocity, 0.02f);
    }

    public void StartRound()
    {
        AllowSpawnServerRPC(true);
        PlayerNetwork[] networks = FindObjectsOfType<PlayerNetwork>();

        GameObject spawnPos = spawnPositions[Random.Range(0, spawnPositions.Length)];
        SetSpawnPositionServerRPC(true, spawnPos.transform.position);

        foreach (PlayerNetwork n in networks)
        {
            if (n == this) continue;
            StartCoroutine(SpawnPosition(spawnPos, n));
            n.AllowSpawnServerRPC(true);
        }
    }
    private IEnumerator SpawnPosition(GameObject position, PlayerNetwork n)
    {
        GameObject pos = spawnPositions[Random.Range(0, spawnPositions.Length)];

        if (pos == position)
        {
            yield return SpawnPosition(position, n);
            yield break;
        }

        n.SetSpawnPositionServerRPC(true, pos.transform.position);
    }


    [ServerRpc(RequireOwnership = false)]
    public void DamageServerRPC(int damage) { healthPoints.Value -= damage; }

    [ServerRpc(RequireOwnership = false)]
    public void CheckDeathServerRPC()  
    {   
        if (healthPoints.Value <= 0)
        {
            Debug.Log(this + "Has Lost!"); 
            StartRound();
        }
        else Debug.Log("false"); 
    }


    [ServerRpc(RequireOwnership = false)]
    public void SetSpawnPositionServerRPC(bool forceSpawn, Vector3 position)
    {
        requireSpawn.Value = forceSpawn;
        spawnPosition.Value = position;
    }

    [ServerRpc(RequireOwnership = false)]
    public void AllowSpawnServerRPC(bool value)
    {
        roundStart.Value = value;
        healthPoints.Value = 10;
    }
}