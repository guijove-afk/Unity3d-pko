using UnityEngine;
using Mirror;
using System.Collections;

public class PlayerRespawn : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private float respawnDelay = 5f;
    [SerializeField] private Vector3 spawnPoint = Vector3.zero;
    [SerializeField] private ParticleSystem respawnEffect;
    [SerializeField] private AudioClip respawnSound;

    private PlayerStats stats;
    private PlayerMovement movement;
    private PlayerCombat combat;

    void Awake()
    {
        stats = GetComponent<PlayerStats>();
        movement = GetComponent<PlayerMovement>();
        combat = GetComponent<PlayerCombat>();
    }

    void Start()
    {
        if (stats != null)
        {
            stats.OnDeath += OnPlayerDeath;
        }
    }

    void OnDestroy()
    {
        if (stats != null)
        {
            stats.OnDeath -= OnPlayerDeath;
        }
    }

    private void OnPlayerDeath()
    {
        if (!isServer) return;

        StartCoroutine(RespawnSequence());
    }

    private IEnumerator RespawnSequence()
    {
        yield return new WaitForSeconds(respawnDelay);

        stats.Revive(true);
    }

    [Client]
    public void RespawnAtSpawnPoint()
    {
        transform.position = spawnPoint;
        transform.rotation = Quaternion.identity;

        if (respawnEffect != null)
        {
            ParticleSystem effect = Instantiate(respawnEffect, transform.position, Quaternion.identity);
            Destroy(effect.gameObject, 3f);
        }

        if (respawnSound != null)
        {
            AudioSource.PlayClipAtPoint(respawnSound, transform.position);
        }

        DamagePopupManager.Instance?.ShowText(transform.position + Vector3.up * 2f, "Ressuscitado!", Color.green);
    }

    [Command]
    public void CmdSetSpawnPoint(Vector3 position)
    {
        spawnPoint = position;
    }

    public void SetSpawnPoint(Vector3 position)
    {
        spawnPoint = position;
    }
}