using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;

public class CameraController : MonoBehaviour
{
    public float MoveSpeed;
    public float ScrollSpeed;
    public float FastSpeedup;
    CinemachineVirtualCamera cinemachineVirtualCamera;

    void Start()
    {
        cinemachineVirtualCamera = GetComponentInChildren<CinemachineVirtualCamera>();
        Assert.IsNotNull(cinemachineVirtualCamera);
    }

    void Update()
    {
        var ori = transform.transform.eulerAngles;
        ori.z = 0f;
        ori.y = math.radians(ori.y);
        ori.x = 0f;
        float activeMoveSpeed = MoveSpeed * (Input.GetKey(KeyCode.LeftShift) ? FastSpeedup : 1);
        Vector3 forward =   math.rotate(float4x4.Euler(ori), math.forward()) * activeMoveSpeed * Time.deltaTime;
        Vector3 right =     math.rotate(float4x4.Euler(ori), math.right()) * activeMoveSpeed * Time.deltaTime;

        if (Input.GetKey(KeyCode.W)) transform.transform.position += forward;
        if (Input.GetKey(KeyCode.S)) transform.transform.position -= forward;
        if (Input.GetKey(KeyCode.D)) transform.transform.position += right;
        if (Input.GetKey(KeyCode.A)) transform.transform.position -= right;

        float mouseScroll = Input.mouseScrollDelta.y;
        float activeScrollSpeed = ScrollSpeed * (Input.GetKey(KeyCode.LeftShift) ? FastSpeedup : 1);

        cinemachineVirtualCamera.GetCinemachineComponent<CinemachineTransposer>().m_FollowOffset -= Vector3.back * mouseScroll * activeScrollSpeed * Time.deltaTime;
    }
}
