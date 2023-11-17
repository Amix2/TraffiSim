using Cinemachine;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;


public class CameraController : MonoBehaviour
{
    public float MoveSpeed;
    public float ScrollSpeed;
    public float FastSpeedup;
    CinemachineVirtualCamera cinemachineVirtualCamera;
    public class ScopedLock : IDisposable { public ScopedLock() { CameraController.lockCamMovement++; }  public void Dispose() { CameraController.lockCamMovement--; }    }
    static public ScopedLock GetScopedLock() { return new ScopedLock(); }
    static int lockCamMovement = 0;
    public static bool skipFrame = false;



    void Start()
    {
        lockCamMovement = 0;
        cinemachineVirtualCamera = GetComponentInChildren<CinemachineVirtualCamera>();
        Assert.IsNotNull(cinemachineVirtualCamera);
    }

    void Update()
    {
        float dt = math.min(Time.deltaTime, 0.1f);
        if (lockCamMovement > 0) return;
        if (skipFrame) return;
        skipFrame = false;

        var ori = transform.transform.eulerAngles;
        ori.z = 0f;
        ori.y = math.radians(ori.y);
        ori.x = 0f;
        float activeMoveSpeed = MoveSpeed * (Input.GetKey(KeyCode.LeftShift) ? FastSpeedup : 1);
        Vector3 forward =   math.rotate(float4x4.Euler(ori), math.forward()) * activeMoveSpeed * dt;
        Vector3 right =     math.rotate(float4x4.Euler(ori), math.right()) * activeMoveSpeed * dt;

        if (Input.GetKey(KeyCode.W)) transform.transform.position += forward;
        if (Input.GetKey(KeyCode.S)) transform.transform.position -= forward;
        if (Input.GetKey(KeyCode.D)) transform.transform.position += right;
        if (Input.GetKey(KeyCode.A)) transform.transform.position -= right;

        float mouseScroll = Input.mouseScrollDelta.y;
        float activeScrollSpeed = ScrollSpeed * (Input.GetKey(KeyCode.LeftShift) ? FastSpeedup : 1);

        CinemachineTransposer cinemachineTransposer = cinemachineVirtualCamera.GetCinemachineComponent<CinemachineTransposer>();
        if(mouseScroll != 0)
            cinemachineTransposer.m_FollowOffset -= Vector3.back * (mouseScroll > 0 ? 1 : -1) * activeScrollSpeed * dt;
    }
}
