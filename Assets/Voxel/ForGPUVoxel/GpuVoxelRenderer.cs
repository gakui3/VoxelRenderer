using System.Collections;
using System.Collections.Generic;
// using System.Numerics;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;

public class GpuVoxelRenderer : MonoBehaviour
{
    [SerializeField] ComputeShader voxelUpdater;
    [SerializeField] Mesh cloneMesh;
    [SerializeField] GameObject sampleObject;
    [SerializeField] Material voxelMaterial;

    ComputeBuffer voxelBuffer;
    ComputeBuffer argBuffer;
    const int voxelSizeOneLine = 20;
    const int voxelCountOneLine = 64; //16 or 32 or 64 or 128
    float voxelScale;
    MeshFilter meshFilter;
    SkinnedMeshRenderer skinnedMeshRenderer;
    List<MeshData> meshDatas;

    Vector4[] voxelVertices = new Vector4[8];
    Vector3[] meshVertices;
    int[] meshIndices;

    uint[] args = new uint[5] { 0, 0, 0, 0, 0, };

    public struct VoxelData
    {
        public Vector3 position;
        public Color color;
        public int isRendering;
    }

    public struct MeshData
    {
        public Vector3 vertex;
    }

    public struct DebugData
    {
        public float3 index;
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
        voxelScale = (float)voxelSizeOneLine / (float)voxelCountOneLine;

        // meshFilter = sampleObject.GetComponent<MeshFilter>();
        // meshIndices = meshFilter.mesh.GetIndices(0);
        // meshVertices = meshFilter.mesh.vertices;

        skinnedMeshRenderer = sampleObject.GetComponent<SkinnedMeshRenderer>();
        meshIndices = skinnedMeshRenderer.sharedMesh.GetIndices(0);
        meshVertices = skinnedMeshRenderer.sharedMesh.vertices;


        voxelBuffer = new ComputeBuffer(voxelCountOneLine * voxelCountOneLine * voxelCountOneLine, Marshal.SizeOf(typeof(VoxelData)));

        VoxelData[] voxelDatas = new VoxelData[voxelCountOneLine * voxelCountOneLine * voxelCountOneLine];
        voxelBuffer.SetData(voxelDatas);

        args[0] = cloneMesh.GetIndexCount(0);
        args[1] = (uint)(voxelCountOneLine * voxelCountOneLine * voxelCountOneLine);
        args[2] = cloneMesh.GetIndexStart(0);
        args[3] = cloneMesh.GetBaseVertex(0);

        argBuffer = new ComputeBuffer(1, sizeof(uint) * args.Length, ComputeBufferType.IndirectArguments);
        argBuffer.SetData(args);

        voxelUpdater.SetBuffer(0, "VoxelBuffer", voxelBuffer);
        voxelUpdater.SetInt("voxelCountOneLine", voxelCountOneLine);
        voxelUpdater.SetInt("voxelSizeOneLine", voxelSizeOneLine);
        voxelUpdater.SetFloat("voxelScale", voxelScale);
        // voxelUpdater.Dispatch(0, voxelCountOneLine / 4, voxelCountOneLine / 4, voxelCountOneLine / 4);
        clearBuffer();

        voxelMaterial.SetBuffer("VoxelBuffer", voxelBuffer);
        voxelMaterial.SetFloat("voxelScale", voxelScale);

        //voxelの頂点を定義
        voxelVertices[0] = new Vector4(0, 0, 0, 0);
        voxelVertices[1] = new Vector4(voxelScale, 0, 0, 0);
        voxelVertices[2] = new Vector4(voxelScale, voxelScale, 0, 0);
        voxelVertices[3] = new Vector4(0, voxelScale, 0, 0);
        voxelVertices[4] = new Vector4(0, 0, voxelScale, 0);
        voxelVertices[5] = new Vector4(voxelScale, 0, voxelScale, 0);
        voxelVertices[6] = new Vector4(voxelScale, voxelScale, voxelScale, 0);
        voxelVertices[7] = new Vector4(0, voxelScale, voxelScale, 0);
    }

    void clearBuffer()
    {
        voxelUpdater.Dispatch(0, voxelCountOneLine / 4, voxelCountOneLine / 4, voxelCountOneLine / 4);
    }

    void updateBuffer()
    {
        clearBuffer();

        // var mesh = meshFilter.sharedMesh;
        // var mesh = skinnedMeshRenderer.sharedMesh;

        Mesh mesh = new Mesh();
        skinnedMeshRenderer.BakeMesh(mesh);

        var indices = mesh.GetIndices(0);
        var vertices = mesh.vertices;
        var meshIndicesLength = meshIndices.Length;
        var matrix = sampleObject.transform.localToWorldMatrix;

        ComputeBuffer vertexBuffer = new ComputeBuffer(vertices.Length, Marshal.SizeOf(typeof(Vector3)));
        ComputeBuffer indexBuffer = new ComputeBuffer(indices.Length, Marshal.SizeOf(typeof(int)));
        vertexBuffer.SetData(vertices);
        indexBuffer.SetData(indices);

        // ComputeBuffer meshBuffer = new ComputeBuffer(indicesLength, Marshal.SizeOf(typeof(MeshData)));
        // meshBuffer.SetData(meshDatas.ToArray());

        // ComputeBuffer debugBuffer = new ComputeBuffer(indicesLength, Marshal.SizeOf(typeof(DebugData)));

        voxelUpdater.SetInt("gridSize", voxelSizeOneLine);
        voxelUpdater.SetBuffer(1, "VoxelBuffer", voxelBuffer);
        voxelUpdater.SetMatrix("LocalToWorldMatrix", matrix);
        voxelUpdater.SetBuffer(1, "VertexBuffer", vertexBuffer);
        voxelUpdater.SetBuffer(1, "IndexBuffer", indexBuffer);
        voxelUpdater.SetVectorArray("VoxelVertices", voxelVertices);
        voxelUpdater.Dispatch(1, meshIndicesLength / 24, 1, 1);

        // DebugData[] debugDatas = new DebugData[indicesLength];
        // debugBuffer.GetData(debugDatas);
        // foreach (var d in debugDatas)
        // {
        //     Debug.Log(d.index);
        // }

        // meshBuffer.Release();
        // debugBuffer.Release();
        vertexBuffer.Release();
        indexBuffer.Release();
    }

    void drawVoxels()
    {
        Graphics.DrawMeshInstancedIndirect(cloneMesh, 0, voxelMaterial, new Bounds(Vector3.zero, Vector3.one * 100), argBuffer);
    }

    void OnDisable()
    {
        voxelBuffer.Release();
        argBuffer.Release();
    }

    void OnDrawGizmos()
    {
        Debug.DrawLine(new Vector3(voxelSizeOneLine * -0.5f, voxelSizeOneLine * -0.5f, voxelSizeOneLine * -0.5f), new Vector3(voxelSizeOneLine * 0.5f, voxelSizeOneLine * -0.5f, voxelSizeOneLine * -0.5f));
        Debug.DrawLine(new Vector3(voxelSizeOneLine * -0.5f, voxelSizeOneLine * -0.5f, voxelSizeOneLine * -0.5f), new Vector3(voxelSizeOneLine * -0.5f, voxelSizeOneLine * -0.5f, voxelSizeOneLine * 0.5f));
        Debug.DrawLine(new Vector3(voxelSizeOneLine * -0.5f, voxelSizeOneLine * -0.5f, voxelSizeOneLine * 0.5f), new Vector3(voxelSizeOneLine * 0.5f, voxelSizeOneLine * -0.5f, voxelSizeOneLine * 0.5f));
        Debug.DrawLine(new Vector3(voxelSizeOneLine * 0.5f, voxelSizeOneLine * -0.5f, voxelSizeOneLine * 0.5f), new Vector3(voxelSizeOneLine * 0.5f, voxelSizeOneLine * -0.5f, voxelSizeOneLine * -0.5f));

        Debug.DrawLine(new Vector3(voxelSizeOneLine * -0.5f, voxelSizeOneLine * -0.5f, voxelSizeOneLine * -0.5f), new Vector3(voxelSizeOneLine * -0.5f, voxelSizeOneLine * 0.5f, voxelSizeOneLine * -0.5f));
        Debug.DrawLine(new Vector3(voxelSizeOneLine * -0.5f, voxelSizeOneLine * -0.5f, voxelSizeOneLine * 0.5f), new Vector3(voxelSizeOneLine * -0.5f, voxelSizeOneLine * 0.5f, voxelSizeOneLine * 0.5f));
        Debug.DrawLine(new Vector3(voxelSizeOneLine * 0.5f, voxelSizeOneLine * -0.5f, voxelSizeOneLine * 0.5f), new Vector3(voxelSizeOneLine * 0.5f, voxelSizeOneLine * 0.5f, voxelSizeOneLine * 0.5f));
        Debug.DrawLine(new Vector3(voxelSizeOneLine * 0.5f, voxelSizeOneLine * -0.5f, voxelSizeOneLine * -0.5f), new Vector3(voxelSizeOneLine * 0.5f, voxelSizeOneLine * 0.5f, voxelSizeOneLine * -0.5f));

        Debug.DrawLine(new Vector3(voxelSizeOneLine * -0.5f, voxelSizeOneLine * 0.5f, voxelSizeOneLine * -0.5f), new Vector3(voxelSizeOneLine * 0.5f, voxelSizeOneLine * 0.5f, voxelSizeOneLine * -0.5f));
        Debug.DrawLine(new Vector3(voxelSizeOneLine * -0.5f, voxelSizeOneLine * 0.5f, voxelSizeOneLine * -0.5f), new Vector3(voxelSizeOneLine * -0.5f, voxelSizeOneLine * 0.5f, voxelSizeOneLine * 0.5f));
        Debug.DrawLine(new Vector3(voxelSizeOneLine * -0.5f, voxelSizeOneLine * 0.5f, voxelSizeOneLine * 0.5f), new Vector3(voxelSizeOneLine * 0.5f, voxelSizeOneLine * 0.5f, voxelSizeOneLine * 0.5f));
        Debug.DrawLine(new Vector3(voxelSizeOneLine * 0.5f, voxelSizeOneLine * 0.5f, voxelSizeOneLine * 0.5f), new Vector3(voxelSizeOneLine * 0.5f, voxelSizeOneLine * 0.5f, voxelSizeOneLine * -0.5f));
    }
}
