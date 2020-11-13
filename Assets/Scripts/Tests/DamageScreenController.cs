using UnityEngine;
using UnityEngine.UI;

public class DamageScreenController : MonoBehaviour
{
    [SerializeField]
    private RawImage damageScreen;
    public Color damageColor;
    public float colorSmoothness = 2f;

    void Start()
    {
        damageScreen.color = Color.clear;
    }
    
    void Update()
    {
        damageScreen.color = Color.Lerp(damageScreen.color, Color.clear, colorSmoothness * Time.deltaTime);
    }

    public void Activate()
    {
        damageScreen.color = damageColor;
    }
}
