using UnityEngine;
using UnityStandardAssets.Utility;

public class Weapon
{
    private float shootTimeOut;
    private float lastShoot;

    public AudioSource audioSource;
    public MuzzleFlash muzzleFlash;
    public GameObject bulletTrail;

    public Weapon(float shootTimeOut, AudioSource audioSource, MuzzleFlash muzzleFlash, GameObject bulletTrail)
    {
        this.shootTimeOut = shootTimeOut;
        this.audioSource = audioSource;
        this.muzzleFlash = muzzleFlash;
        this.bulletTrail = bulletTrail;
        lastShoot = -1f;
    }

    public bool Shoot(float shootTime)
    {
        if (shootTime - lastShoot > shootTimeOut)
        {
            audioSource.Play();
            muzzleFlash.PlayMuzzleFlash();
            lastShoot = shootTime;
            return true;
        }

        return false;
    }

    public void SpawnBullet(Vector3 shootPosition, Vector3 hitPosition)
    {
        GameObject bulletTrailEffect =
            GameObject.Instantiate(bulletTrail, shootPosition, Quaternion.identity);
        LineRenderer lineRenderer = bulletTrail.GetComponent<LineRenderer>();
        lineRenderer.SetPosition(0, shootPosition);
        lineRenderer.SetPosition(1, hitPosition);
        GameObject.Destroy(bulletTrailEffect, 1f);
    }
}
