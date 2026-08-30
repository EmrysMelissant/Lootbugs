using UnityEngine;
using Unity.Netcode;
public class FlashLight : NetworkBehaviour
{
    public GameObject flashLight;
    public KeyCode lightKey = KeyCode.F;
    void Start()
    {
        flashLight.active = false;
    }

    // Update is called once per frame
    void Update()
    {
        if(!IsOwner) return;
        if (Input.GetKeyDown(lightKey))
        {
            if(flashLight.active == true)
            {
                flashLight.active = false;
            }
            else
            {
                flashLight.active = true;
            }
            
        }
    }
}
