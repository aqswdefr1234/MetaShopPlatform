using UnityEngine;
public class HumanController : MonoBehaviour//Only MyPlayerObject
{
    public int _mouseSpeed = 10;
    internal int _moveSpeed = 10;
    internal int _jumpPower = 300;

    private Transform body;
    private Transform cameraPos;
    private Transform playerCam;
    private Rigidbody rigid;
    private float _xRotate = 0f;
    private float _yRotate = 0f;
    
    //LimitState
    internal bool canMove = true;
    internal bool canRotate = true;
    void Start()
    {
        body = FindTransform.FindChild(transform, "RigidBody");
        rigid = body.GetComponent<Rigidbody>();
        cameraPos = FindTransform.FindChild(body, "CameraPos");
        playerCam = GameObject.Find("PlayerCamera").transform;
    }
    void Update()
    {
        Move();
        Jump();
        CameraPos();
        Look();
    }
    private Transform FindChildTrans(Transform target, string targetName)
    {
        foreach (Transform child in target)
            if (child.name == targetName) return child;
        return null;
    }
    private void Move()
    {
        if (canMove == false) return;
        float horizon = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        float distance = _moveSpeed * Time.deltaTime;
        body.Translate(new Vector3(horizon * distance, 0, vertical * distance));
    }
    private void Jump()
    {
        if (Input.GetKeyDown(KeyCode.Space) && DetectGround.isAttach)
        {
            rigid.AddForce(Vector3.up * _jumpPower, ForceMode.Impulse);
        }
    }
    private void CameraPos()
    {
        playerCam.position = cameraPos.position;
    }
    private void Look()
    {
        if (!canRotate) return;
        float horizontal = Input.GetAxis("Mouse X") * _mouseSpeed;
        float vertical = -Input.GetAxis("Mouse Y") * _mouseSpeed;

        _xRotate = Mathf.Clamp(_xRotate + vertical, -45, 80);
        _yRotate = body.eulerAngles.y + horizontal;

        playerCam.eulerAngles = new Vector3(_xRotate, _yRotate, 0);
        body.eulerAngles = new Vector3(0, _yRotate, 0);
    }
}
