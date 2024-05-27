using UnityEngine.EventSystems;
using UnityEngine;

public class MouseController : MonoBehaviour
{
    [SerializeField] private Transform cameraTrans;
    [SerializeField] private Transform xyzObject;

    public float screenSpeed = 10f;
    public float scrollSpeed = 1000f;
    public float rotateSpeed = 500f;

    private Transform preTrans = null;
    private Transform selectedObject
    {
        get { return preTrans; }
        set { ChangeLayer(value); TransferXYZ(value);}
    }
    //Press Left
    bool isPressXYZ = false; Transform pressedXYZ = null; Vector3 diffMO = new Vector3();

    void Update()
    {
        ControllMouse();
    }

    void ChangeLayer(Transform curTran)
    {
        if (curTran == preTrans) return;
        if (curTran == null)
        {
            preTrans.gameObject.layer = LayerMask.NameToLayer("RayCastLayer");
            preTrans = curTran;
            return;
        }

        SendChangedTransData(curTran);
        if (preTrans == null)
        {
            curTran.gameObject.layer = LayerMask.NameToLayer("CurrentSelected");
            preTrans = curTran;
            return;
        }
        preTrans.gameObject.layer = LayerMask.NameToLayer("RayCastLayer");//�ٽ� ���ð����ϵ��� ���̾ �ٲ��ش�.
        curTran.gameObject.layer = LayerMask.NameToLayer("CurrentSelected");//���ο� ������Ʈ���� CurrentSelected ���̾� �Ҵ�
        preTrans = curTran;
    }
    void TransferXYZ(Transform curTrans)
    {
        if (curTrans == null) xyzObject.position = new Vector3(1000f, 1000f, 1000f);
        else xyzObject.position = curTrans.position;
    }
    void SendChangedTransData(Transform changeTrans)
    {
        transform.GetComponent<ObjectsController>().LoadData(changeTrans);
    }
    void ControllMouse()
    {
        ClickPressLeft();
        PressWheel();
        PressRight();
        ScrollWheel();
    }
    //-----���콺 ���ʹ�ư ��Ʈ��-----//
    void ClickPressLeft()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current.IsPointerOverGameObject()) return;//���콺�� UI���� ���� ��
            
            int layerMask = 1 << LayerMask.NameToLayer("RayCastLayer");
            Transform rayHitTrans = RayHitTransform(layerMask);

            if (rayHitTrans == null) { selectedObject = rayHitTrans; return; }
            if (rayHitTrans.tag == "xyz")
            {
                isPressXYZ = true; pressedXYZ = rayHitTrans;
                diffMO = GetMouseWorldPos() - selectedObject.position; 
                return; 
            }
            selectedObject = rayHitTrans;
        }
        if (Input.GetMouseButtonUp(0)) 
        {
            if (isPressXYZ) { isPressXYZ = false; pressedXYZ = null; }
        }
        if (isPressXYZ) PressXYZ(pressedXYZ);
    }
    Transform RayHitTransform(int layerMask)
    {
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out hit, Mathf.Infinity, layerMask)) return null;
        return hit.transform;
    }
    void PressXYZ(Transform xyz)
    {
        if (selectedObject == null) return;
        Vector3 newPos = GetMouseWorldPos();
        if (xyz.name == "X")
        {
            selectedObject.position = new Vector3(newPos.x - diffMO.x, selectedObject.position.y, selectedObject.position.z);
        }
        else if (xyz.name == "Y")
        {
            selectedObject.position = new Vector3(selectedObject.position.x, newPos.y - diffMO.y, selectedObject.position.z);
        }
        else if (xyz.name == "Z")
        {
            selectedObject.position = new Vector3(selectedObject.position.x, selectedObject.position.y, newPos.z - diffMO.z);
        }
        xyzObject.position = selectedObject.position;
    }
    private Vector3 GetMouseWorldPos()
    {
        float mZCoord = Camera.main.WorldToScreenPoint(selectedObject.position).z;
        Vector3 mousePoint = Input.mousePosition;
        mousePoint.z = mZCoord;
        return Camera.main.ScreenToWorldPoint(mousePoint);
    }
    //------------------------------------//

    void PressRight()// ���콺 ������ ��ư Ŭ��. ī�޶� ȸ��
    {
        if (Input.GetMouseButton(1))
        {
            float mouseX = Input.GetAxis("Mouse X") * rotateSpeed * Time.deltaTime;
            float mouseY = Input.GetAxis("Mouse Y") * rotateSpeed * Time.deltaTime;
            cameraTrans.Rotate(Vector3.up, mouseX, Space.World);
            cameraTrans.Rotate(Vector3.right, -mouseY, Space.Self);
        }
    }
    void PressWheel()//�� ������������
    {
        if (Input.GetMouseButton(2))
        {
            float horizontal = Input.GetAxis("Mouse X") * screenSpeed * Time.deltaTime;
            float vertical = Input.GetAxis("Mouse Y") * screenSpeed * Time.deltaTime;
            cameraTrans.Translate(new Vector3(-horizontal, -vertical, 0));
        }
    }
    void ScrollWheel()// ī�޶� Ȯ��/���
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel") * scrollSpeed * Time.deltaTime;
        cameraTrans.Translate(new Vector3(0, 0, scroll), Space.Self);
    }
}

