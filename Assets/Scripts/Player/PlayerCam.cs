using Unity.Netcode;
using UnityEngine;
public class PlayerCam : NetworkBehaviour
{
    public float senseX;
    public float senseY;
    public Transform orientation;
    public GameObject camera;
    float xRotation;
    float yRotation;

    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsOwner)
        {
            if (camera != null)
            {
                camera.SetActive(false);
            }
            if (TryGetComponent(out AudioListener listener))
            {
                listener.enabled = false;
            }
            else if (camera != null && camera.TryGetComponent(out AudioListener camListener))
            {
                camListener.enabled = false;
            }
            return;
        }

        if (camera != null)
        {
            camera.SetActive(true);
        }
    }

    void Start()
    {
        if (!IsOwner) return;

#if !UNITY_ANDROID && !UNITY_IOS
        if (!Application.isMobilePlatform)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
#endif
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsOwner) return;

        float deltaX = 0f;
        float deltaY = 0f;

        // Desktop mouse input
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            deltaX += Input.GetAxisRaw("Mouse X") * Time.deltaTime * senseX;
            deltaY += Input.GetAxisRaw("Mouse Y") * Time.deltaTime * senseY;
        }

        // Mobile touch look input
        if (MobileInputManager.Instance != null)
        {
            Vector2 touchLook = MobileInputManager.Instance.GetLookDelta();
            deltaX += touchLook.x;
            deltaY += touchLook.y;
        }

        yRotation += deltaX;
        xRotation -= deltaY;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);

        if (camera != null)
        {
            camera.transform.localRotation = Quaternion.Euler(xRotation, yRotation, 0);
        }
        if (orientation != null)
        {
            orientation.localRotation = Quaternion.Euler(0, yRotation, 0);
        }
    }
}

