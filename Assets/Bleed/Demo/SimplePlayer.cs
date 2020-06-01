using UnityEngine;
using System.Collections;

public class SimplePlayer : MonoBehaviour 
{
    [SerializeField]
    private float maxHP = 100;
    private float HP = 100;

    [SerializeField]
    private float damageBloodAmount = 3; //amount of blood when taking damage (relative to damage taken (relative to HP remaining))
    [SerializeField]
    private float maxBloodIndication = 0.5f; //max amount of blood when not taking damage (relative to HP lost)

    [SerializeField]
    float recoverSpeed = 1;//HP per second

	void Start () 
    {
        HP = maxHP;
	}

    void Update ()
    {
        HP += recoverSpeed * Time.deltaTime;
        if (HP > maxHP)
        {
            HP = maxHP;
        }
        BleedBehavior.minBloodAmount = maxBloodIndication * (maxHP - HP) / maxHP;
    }

    public void Damage(int amount)
    {
        BleedBehavior.BloodAmount += Mathf.Clamp01(damageBloodAmount * amount / HP);

        HP -= amount;

        if (HP <= 0)
        {
            HP = maxHP;
            Debug.Log("Player died, resetting HP");
        }
        BleedBehavior.minBloodAmount = maxBloodIndication * (maxHP - HP) / maxHP;
    }

    void OnGUI()
    {
        GUI.Label(new Rect(50, 50, 100, 20), "HP: " + Mathf.FloorToInt(HP));
    }
}
