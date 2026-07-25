using System;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private float damage = 20f;
    [SerializeField] private float range = 100f;
    [Tooltip("Layers the shot can hit, set to your enemy + environment layers")]
    [SerializeField] private LayerMask hitMask = ~0;

    [Header("Fire")]
    [Tooltip("Shots per second")]
    [SerializeField] private float fireRate = 8f;
    [SerializeField] private bool automatic = true;

    [Header("Ammo")]
    [SerializeField] private int magazineSize = 30;
    [SerializeField] private int reserveAmmo = 90;
    [SerializeField] private float reloadTime = 1.6f;

    [Header("Feedback (optional)")]
    [Tooltip("Muzzle flash / fire sound, played on each shot")]
    [SerializeField] private ParticleSystem muzzleFlash;
    [SerializeField] private AudioSource fireSound;
    [Tooltip("Small effect spawned where the shot lands")]
    [SerializeField] private GameObject hitEffect;

    public int CurrentAmmo { get; private set; }
    public int ReserveAmmo => reserveAmmo;
    public bool IsReloading { get; private set; }

    public event Action OnAmmoChanged;

    private Camera cam;
    private float nextFireTime;
    private float reloadFinishTime;

    void Awake()
    {
        CurrentAmmo = magazineSize;
    }

    void Update()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        if (IsReloading)
        {
            if (Time.time >= reloadFinishTime) FinishReload();
            return;
        }

        if (Input.GetKeyDown(KeyCode.R)) { StartReload(); return; }

        var wantsToFire = automatic ? Input.GetMouseButton(0) : Input.GetMouseButtonDown(0);
        if (wantsToFire && Time.time >= nextFireTime) Fire();
    }

    void Fire()
    {
        if (CurrentAmmo <= 0)
        {
            StartReload();
            return;
        }

        nextFireTime = Time.time + 1f / Mathf.Max(0.01f, fireRate);
        CurrentAmmo--;
        OnAmmoChanged?.Invoke();

        if (muzzleFlash != null) muzzleFlash.Play();
        if (fireSound != null) fireSound.Play();

        var ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out var hit, range, hitMask, QueryTriggerInteraction.Ignore))
        {
            var enemy = hit.collider.GetComponentInParent<EnemyHealth>();
            if (enemy != null) enemy.TakeDamage(damage);

            if (hitEffect != null)
                Destroy(Instantiate(hitEffect, hit.point, Quaternion.LookRotation(hit.normal)), 2f);
        }
    }

    void StartReload()
    {
        if (IsReloading || CurrentAmmo >= magazineSize || reserveAmmo <= 0) return;
        IsReloading = true;
        reloadFinishTime = Time.time + reloadTime;
    }

    void FinishReload()
    {
        IsReloading = false;
        var needed = magazineSize - CurrentAmmo;
        var taken = Mathf.Min(needed, reserveAmmo);
        CurrentAmmo += taken;
        reserveAmmo -= taken;
        OnAmmoChanged?.Invoke();
    }
}
