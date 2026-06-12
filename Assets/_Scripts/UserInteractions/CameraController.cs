using Unity.Cinemachine;
using Unity.Mathematics;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float MoveSpeed = 1f;    // [2*screens/s]
    public float RotationSpeed = 180f; // [degrees/s]
    public float ZoomSpeed = 1f;    

    public CinemachineCamera Camera;


    private InputSystem_Actions InputActions;

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        // dt normalized
        float2 UserMovement = InputActions.Player.Move.ReadValue<Vector2>() * Time.deltaTime * MoveSpeed;
        float2 UserRotation = InputActions.Player.Rotate.ReadValue<Vector2>() * Time.deltaTime * RotationSpeed;

        Vector2 UserScroll = InputActions.Player.Zoom.ReadValue<Vector2>() * Time.deltaTime * ZoomSpeed;
        if(Camera.Lens.ModeOverride == LensSettings.OverrideModes.Orthographic)
        {
            Camera.Lens.OrthographicSize -= UserScroll.y * math.sqrt(Camera.Lens.OrthographicSize + 1);
            Camera.Lens.OrthographicSize = math.clamp(Camera.Lens.OrthographicSize, 0.1f, 10000f);
            UserMovement *= Camera.Lens.OrthographicSize;
        }
        transform.Translate(new Vector3(UserMovement.x, 0, UserMovement.y), Space.Self);
        transform.RotateAround(transform.position, Vector3.up, UserRotation.x);

    }

    private void Awake()
    {
        InputActions = new InputSystem_Actions();
    }

    private void OnEnable()
    {
        InputActions.Player.Enable();
    }

    private void OnDisable()
    {
        InputActions.Player.Disable();
    }
}
