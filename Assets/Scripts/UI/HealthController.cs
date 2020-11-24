using UnityEngine;
using UnityEngine.UI;

public class HealthController : MonoBehaviour
{
    [SerializeField]
    private Text healthValue;
    [SerializeField]
    private RawImage heartImage;
    public Color fullLife;
    public Color halfLife;
    public Color lowLife;
    public Color emptyLife;
    
    private float life;

    void Start()
    {
        life = 100f;
        healthValue.color = fullLife;
        healthValue.text = "100";
        heartImage.color = fullLife;
    }
    void Update()
    {
        Color currentColor = GetColorForLifeValue();
        healthValue.color = currentColor;
        healthValue.text = ((int) (life + 0.5f)).ToString();
        heartImage.color = currentColor;
    }

    private Color GetColorForLifeValue()
    {
        Color expectedColor;
        if (life >= 80.0f)
        {
            expectedColor = fullLife;
        }
        else if (life >= 50.0f)
        {
            expectedColor = halfLife;
        }
        else if (life >= 25.0f)
        {
            expectedColor = lowLife;
        }
        else
        {
            expectedColor = emptyLife;
        }

        return expectedColor;
    }

    public void UpdateLife(float newLife)
    {
        life = newLife;
    }
}
