using Mirror;
using UnityEngine;

[RequireComponent(typeof(HitFlashEffect))]
public class Health : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnHpChanged))]
    private int hp = 100;

    public int maxHp = 100;
    public HPBarWorld hpBarPrefab;
    public GameObject damagePopupPrefab;
    public GameObject hitEffectPrefab;

    private HPBarWorld hpBarInstance;
    private HitFlashEffect hitFlashEffect;

    public int Hp => hp;
    public int MaxHp => maxHp;

    private void Awake()
    {
        hitFlashEffect = GetComponent<HitFlashEffect>();
    }

    public override void OnStartServer()
    {
        hp = maxHp;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        CreateHpBar();
        UpdateHpBar(hp);
    }

    public override void OnStopClient()
    {
        if (hpBarInstance != null)
        {
            Destroy(hpBarInstance.gameObject);
            hpBarInstance = null;
        }
    }

    [Server]
    public void TakeDamage(int damage)
    {
        Debug.Log("TakeDamage chamado: " + damage);

        if (hp <= 0)
            return;

        hp = Mathf.Max(hp - damage, 0);

        Debug.Log("HP atual: " + hp);

        RpcShowDamage(damage);

        if (hp <= 0)
            Die();
    }

    [Server]
    private void Die()
    {
        NetworkServer.Destroy(gameObject);
    }

    private void OnHpChanged(int oldHp, int newHp)
    {
        UpdateHpBar(newHp);

        if (hpBarInstance != null && oldHp != newHp)
            hpBarInstance.ShowForSeconds();

        if (newHp < oldHp)
        {
            if (hitFlashEffect != null)
                hitFlashEffect.PlayFlash();

            if (isLocalPlayer)
            {
                ScreenHitFlash.EnsureExists();
                if (ScreenHitFlash.Instance != null)
                    ScreenHitFlash.Instance.Flash();
            }
        }
    }

    private void CreateHpBar()
    {
        if (!isClient || hpBarPrefab == null || hpBarInstance != null)
            return;

        hpBarInstance = Instantiate(hpBarPrefab);
        hpBarInstance.Bind(this, transform);
    }

    private void UpdateHpBar(int currentHp)
    {
        if (hpBarInstance == null)
            return;

        hpBarInstance.SetHealth(currentHp, maxHp);
    }

    [ClientRpc]
    private void RpcShowDamage(int damage)
    {
        if (!isClient)
            return;

        SpawnHitEffect();

        if (DamagePopupManager.Instance == null)
            return;

        DamagePopupManager.Instance.ShowDamage(damage, transform);
    }

    private void SpawnHitEffect()
    {
        if (hitEffectPrefab == null)
            return;

        Vector3 spawnPosition = transform.position + Vector3.up * 1.1f;
        GameObject instance = Instantiate(hitEffectPrefab, spawnPosition, Quaternion.identity);

        if (instance.GetComponent<OneShotAutoDestroy>() == null)
            instance.AddComponent<OneShotAutoDestroy>();
    }
}
