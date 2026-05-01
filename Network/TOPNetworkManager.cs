using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;

public class TOPNetworkManager : NetworkManager
{
    [Header("Tales of Pirates")]
    [SerializeField] private Transform[] spawnPoints;
   // [SerializeField] private GameObject playerPrefab;
    [SerializeField] private string gameScene = "GameScene";
    [SerializeField] private string loginScene = "LoginScene";

    [Header("Character Selection")]
    [SerializeField] private int maxCharactersPerAccount = 3;

    public static TOPNetworkManager Instance => singleton as TOPNetworkManager;

    public override void OnStartServer()
    {
        base.OnStartServer();
    }

public override void OnServerAddPlayer(NetworkConnectionToClient conn)
{
    Transform spawnPoint = GetSpawnPoint();

    GameObject player = Instantiate(
        playerPrefab, // ← esse já vem do NetworkManager base
        spawnPoint.position,
        spawnPoint.rotation
    );

    NetworkServer.AddPlayerForConnection(conn, player);
}

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        if (conn.identity != null)
        {
            if (conn.identity.TryGetComponent(out PlayerStats stats))
            {
                SavePlayerData(conn.identity);
            }
        }

        base.OnServerDisconnect(conn);
    }

    private Transform GetSpawnPoint()
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            return spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)];
        }

        GameObject spawnObj = new GameObject("SpawnPoint");
        return spawnObj.transform;
    }

    private void SavePlayerData(NetworkIdentity player)
    {
        // Salvar no banco de dados
    }

    public void StartGame()
    {
        ServerChangeScene(gameScene);
    }

    public void ReturnToLogin()
    {
        ServerChangeScene(loginScene);
    }
}