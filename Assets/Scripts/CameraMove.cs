using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CameraMove : MonoBehaviour
{
    private const float cameraSpeed = 3.0f;
    private const float moveSpeed = 7.5f;
    public Quaternion TargetRotation { private set; get; }
    private Vector3 moveVector = Vector3.zero;
    private float moveY = 0.0f;
    private new Rigidbody rigidbody;
    private void Awake()
    {
        // 속도 변경을 위한 리지드바디 달아줌
        rigidbody = GetComponent<Rigidbody>();

        Cursor.lockState = CursorLockMode.Locked;

        TargetRotation = transform.rotation;
    }

    private void Update()
    {
        // 마우스 축으로 회전
        var rotation = new Vector2(-Input.GetAxis("Mouse Y"), Input.GetAxis("Mouse X"));
        
        // 회전속도 고려
        var targetEuler = TargetRotation.eulerAngles + (Vector3)rotation * cameraSpeed;

        // 180도 넘어가면 -360으로 보정
        if(targetEuler.x > 180.0f)
        {
            targetEuler.x -= 360.0f;
        }

        // 좌우 75도 이상 넘어가지 않게
        targetEuler.x = Mathf.Clamp(targetEuler.x, -75.0f, 75.0f);
        TargetRotation = Quaternion.Euler(targetEuler);

        // 구면 선형 보간 사용해서 회전 처리
        transform.rotation = Quaternion.Slerp(transform.rotation, TargetRotation, 
            Time.deltaTime * 15.0f);

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        moveVector = new Vector3(x, 0.0f, z) * moveSpeed;

        moveY = Input.GetAxis("Elevation");

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SceneManager.LoadScene(0);
        }
    }

    private void FixedUpdate()
    {
        Vector3 newVelocity = transform.TransformDirection(moveVector);
        newVelocity.y += moveY * moveSpeed;
        rigidbody.velocity = newVelocity;
    }

    // 포탈 타면 카메라 상하 초기화
    public void ResetTargetRotation()
    {
        TargetRotation = Quaternion.LookRotation(transform.forward, Vector3.up);
    }

    
}
