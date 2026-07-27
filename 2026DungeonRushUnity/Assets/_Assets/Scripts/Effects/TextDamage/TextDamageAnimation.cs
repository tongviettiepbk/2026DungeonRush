using System.Collections;
using UnityEngine;


public class TextDamageAnimation : MonoBehaviour
{
    public CombatText textDamage;

    public void OnAnimationEnd()
    {
        textDamage.Deactive();
    }
}
