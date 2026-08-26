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
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsOwner) return;

        bool isMobile = Application.isMobilePlatform;
        if (!isMobile && Cursor.lockState != CursorLockMode.Locked) return;

        float lookDeltaX = 0f;
        float lookDeltaY = 0f;

        if (MobileMotionManager.Instance != null)
        {
            Vector2 look = MobileMotionManager.Instance.GetLookDelta(senseX, senseY);
            lookDeltaX = look.x;
            lookDeltaY = look.y;
        }
        else
        {
            lookDeltaX = Input.GetAxisRaw("Mouse X") * Time.deltaTime * senseX;
            lookDeltaY = Input.GetAxisRaw("Mouse Y") * Time.deltaTime * senseY;
        }

        yRotation += lookDeltaX;
        xRotation -= lookDeltaY;
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
