using UnityEngine;

public class Weapon
{
    private float shootTimeOut;
    private float lastShoot;

    public AudioSource audioSource;
    public MuzzleFlash muzzleFlash;

    public Weapon(float shootTimeOut, AudioSource audioSource, MuzzleFlash muzzleFlash)
    {
        this.shootTimeOut = shootTimeOut;
        this.audioSource = audioSource;
        this.muzzleFlash = muzzleFlash;
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
}
