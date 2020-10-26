using UnityEngine;

public class MuzzleFlash : MonoBehaviour
{
    public ParticleSystem muzzleFlash;

    public void PlayMuzzleFlash()
    {
        muzzleFlash.Play();
    }
}
