using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class VoxelRenderer : MonoBehaviour
{
    [SerializeField] ComputeShader voxelUpdater;
    [SerializeField] Mesh meshData;
    [SerializeField] Material voxelMaterial;

    ComputeBuffer voxelBuffer;
    ComputeBuffer argBuffer;
    int gridSize = 64;
    uint[] args = new uint[5] { 0, 0, 0, 0, 0, };

    Texture2D envDepthTexture;

    public struct VoxelData
    {
        public Vector3 position;
        public Color color;
    }
    void Start()
    {
        initBuffer();
    }

    void Update()
    {
        updateBuffer();
        drawVoxels();
    }

    void initBuffer()
    {
        voxelBuffer = new ComputeBuffer(gridSize * gridSize * gridSize, Marshal.SizeOf(typeof(VoxelData)));

        VoxelData[] voxelDatas = new VoxelData[gridSize * gridSize * gridSize];
        voxelBuffer.SetData(voxelDatas);

        args[0] = meshData.GetIndexCount(0);
        args[1] = (uint)(gridSize * gridSize * gridSize);
        args[2] = meshData.GetIndexStart(0);
        args[3] = meshData.GetBaseVertex(0);

        argBuffer = new ComputeBuffer(1, sizeof(uint) * args.Length, ComputeBufferType.IndirectArguments);
        argBuffer.SetData(args);

        voxelUpdater.SetBuffer(0, "VoxelBuffer", voxelBuffer);
        voxelUpdater.SetInt("gridSize", gridSize);
        voxelUpdater.Dispatch(0, gridSize / 8, gridSize / 8, gridSize / 8);

        voxelMaterial.SetBuffer("VoxelBuffer", voxelBuffer);
    }

    void updateBuffer()
    {
        voxelUpdater.SetInt("gridSize", gridSize);

        voxelUpdater.SetBuffer(1, "VoxelBuffer", voxelBuffer);
        voxelUpdater.Dispatch(1, gridSize / 8, gridSize / 8, gridSize / 8);
    }

    void drawVoxels()
    {
        Graphics.DrawMeshInstancedIndirect(meshData, 0, voxelMaterial, new Bounds(Vector3.zero, Vector3.one * 100), argBuffer);
    }

    // void OnGUI()
    // {
        // envDepthTexture = occlusionManager.environmentDepthTexture;
        // GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), envDepthTexture);
    // }
}
