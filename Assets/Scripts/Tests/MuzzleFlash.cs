using UnityEngine;

public class MuzzleFlash : MonoBehaviour
{
    public ParticleSystem muzzleFlash;
    public GameObject gunEnd;
    public void PlayMuzzleFlash()
    {
        muzzleFlash.Play();
    }

    public Vector3 GetShootPosition()
    {
        return gunEnd.transform.position;
    }
}
