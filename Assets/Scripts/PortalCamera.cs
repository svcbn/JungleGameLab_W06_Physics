using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using RenderPipeline = UnityEngine.Rendering.RenderPipelineManager;

public class PortalCamera : MonoBehaviour
{
    [SerializeField]
    private Portal[] portals = new Portal[2];

    [SerializeField]
    private Camera portalCamera;

    [SerializeField]
    private int iterations = 7;

    private RenderTexture tempTexture1;
    private RenderTexture tempTexture2;

    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = GetComponent<Camera>();

        // 포탈면에 씌울 텍스쳐
        tempTexture1 = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
        tempTexture2 = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
    }

    private void Start()
    {
        // 생성한 텍스쳐 포탈면 material로 씌워주기
        portals[0].Renderer.material.mainTexture = tempTexture1;
        portals[1].Renderer.material.mainTexture = tempTexture2;
    }

    private void OnEnable()
    {
        RenderPipeline.beginCameraRendering += UpdateCamera;
    }

    private void OnDisable()
    {
        RenderPipeline.beginCameraRendering -= UpdateCamera;
    }

    // 텍스쳐 업데이트
    void UpdateCamera(ScriptableRenderContext SRC, Camera camera)
    {
        // 포탈 둘중 하나라도 없으면 return
        if (!portals[0].IsPlaced || !portals[1].IsPlaced)
        {
            return;
        }

        // 포탈1의 렌더러가 켜졌을때
        if (portals[0].Renderer.isVisible)
        {
            // 포탈 텍스쳐 설정
            portalCamera.targetTexture = tempTexture1;

            // 렌더링 반복시켜주기
            for (int i = iterations - 1; i >= 0; --i)
            {
                RenderCamera(portals[0], portals[1], i, SRC);
            }
        }

        // 포탈2의 렌더러가 켜졌을때
        if(portals[1].Renderer.isVisible)
        {
            // 포탈 텍스쳐 설정
            portalCamera.targetTexture = tempTexture2;

            // 렌더링 반복시켜주기
            for (int i = iterations - 1; i >= 0; --i)
            {
                RenderCamera(portals[1], portals[0], i, SRC);
            }
        }
    }

    // 카메라 렌더링 해주는 파트
    private void RenderCamera(Portal inPortal, Portal outPortal, int iterationID, ScriptableRenderContext SRC)
    {
        Transform inTransform = inPortal.transform;
        Transform outTransform = outPortal.transform;

        Transform cameraTransform = portalCamera.transform;
        cameraTransform.position = transform.position;
        cameraTransform.rotation = transform.rotation;

        for(int i = 0; i <= iterationID; ++i)
        {
            // 포탈카메라 위치를 인포탈의 상대위치로 바꿔서
            Vector3 relativePos = inTransform.InverseTransformPoint(cameraTransform.position);
            
            // 쿼터니언을 반대로 뒤집고(반전시키고)
            relativePos = Quaternion.Euler(0.0f, 180.0f, 0.0f) * relativePos;
            
            // 다시 아웃포탈쪽의 월드위치로 바꿔준다
            cameraTransform.position = outTransform.TransformPoint(relativePos);

            // 회전에 대해서도 앞과 같은 처리
            Quaternion relativeRot = Quaternion.Inverse(inTransform.rotation) * cameraTransform.rotation;
            relativeRot = Quaternion.Euler(0.0f, 180.0f, 0.0f) * relativeRot;
            cameraTransform.rotation = outTransform.rotation * relativeRot;
        }

        // 새로운 렌더링 평면 생성
        Plane p = new Plane(-outTransform.forward, outTransform.position);

        // 새 평면의 법선벡터들과 거리로 월드좌표상 평면이 존재하는 벡터4 설정
        Vector4 clipPlaneWorldSpace = new Vector4(p.normal.x, p.normal.y, p.normal.z, p.distance);

        // 4X4 행렬의 역행렬 구하고(위치 역산) 전치(대칭) 계산 
        Vector4 clipPlaneCameraSpace =
            Matrix4x4.Transpose(Matrix4x4.Inverse(portalCamera.worldToCameraMatrix)) * clipPlaneWorldSpace;

        // 경사 근접 평면 투영 행렬을 계산해서 반환
        var newMatrix = mainCamera.CalculateObliqueMatrix(clipPlaneCameraSpace);

        // 계산된 투영 행렬 반영
        portalCamera.projectionMatrix = newMatrix;

        // 포탈 카메라에 렌더링
        UniversalRenderPipeline.RenderSingleCamera(SRC, portalCamera);
    }
}
