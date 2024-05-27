using UnityEngine;

public class PlayerController_Mobile : MonoBehaviour//Only MyPlayerObject
{
    [SerializeField] private GameObject mobileCanvas;
    public int _rotateSpeed = 30;
    internal int _moveSpeed = 10;
    internal int _jumpPower = 300;

    private Transform body, cameraPos, playerCam;
    private Transform moveHandle, rotateHandle, jumpBtn;
    private Rigidbody rigid;
    private float _xRotate = 0f;
    private float _yRotate = 0f;

    void Start()
    {
        mobileCanvas.SetActive(true);
        SetTransform();
    }
    void SetTransform()
    {
        body = FindTransform.FindChild(transform, "RigidBody");
        rigid = body.GetComponent<Rigidbody>();
        cameraPos = FindTransform.FindChild(body, "CameraPos");
        playerCam = GameObject.Find("PlayerCamera").transform;

        //Mobile Canvas
        moveHandle = FindTransform.FindChild(mobileCanvas.transform, "MoveJoystick").GetChild(0);
        rotateHandle = FindTransform.FindChild(mobileCanvas.transform, "RotateJoystick").GetChild(0).GetChild(0);
        jumpBtn = FindTransform.FindChild(mobileCanvas.transform, "JumpButton");
        jumpBtn.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(Jump);
    }
    void Update()
    {
        Vector2 moveDir = new Vector2(moveHandle.localPosition.x, moveHandle.localPosition.y).normalized; ;
        Vector2 rotDir = new Vector2(rotateHandle.localPosition.x, rotateHandle.localPosition.y).normalized;

        Move(moveDir);
        CameraPos();
        Look(rotDir);
    }
    private void Move(Vector2 moveDir)
    {
        float distance = _moveSpeed * Time.deltaTime;
        body.Translate(new Vector3(moveDir.x * distance, 0, moveDir.y * distance));
    }
    private void Jump()
    {
        if (DetectGround.isAttach)
        {
            rigid.AddForce(Vector3.up * _jumpPower, ForceMode.Impulse);
        }
    }
    private void CameraPos()
    {
        playerCam.position = cameraPos.position;
    }
    private void Look(Vector2 rotDir)
    {
        float horizontal = rotDir.x * Time.deltaTime * _rotateSpeed;
        float vertical = -rotDir.y * Time.deltaTime * _rotateSpeed;

        _xRotate = Mathf.Clamp(_xRotate + vertical, -45, 80);
        _yRotate = body.eulerAngles.y + horizontal;

        playerCam.eulerAngles = new Vector3(_xRotate, _yRotate, 0);
        body.eulerAngles = new Vector3(0, _yRotate, 0);
    }
}