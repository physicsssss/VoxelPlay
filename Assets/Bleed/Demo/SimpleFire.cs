using UnityEngine;
using System.Collections;

public class SimpleFire : MonoBehaviour
{
    [SerializeField]
    private int damage = 10;
    [SerializeField]
    private float damageDelay = 1;
    private float lastDamageTime = 0;

    void OnTriggerStay(Collider other)
    {
        DoDamage(other);
    }

    void DoDamage(Collider other)
    {
        if (Time.time > lastDamageTime + damageDelay)
        {
            other.SendMessageUpwards("Damage", damage);
            lastDamageTime = Time.time;
        }
    }
}
