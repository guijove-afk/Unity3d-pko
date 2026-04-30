using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class HPBar : MonoBehaviour
{
    public Slider slider;
    private Health target;

    void Update()
    {
        if (target != null && slider != null)
        {
            slider.minValue = 0f;
            slider.maxValue = target.MaxHp;
            slider.value = target.Hp;
        }
    }

    public void SetTarget(Health newTarget)
    {
        target = newTarget;
    }
}
