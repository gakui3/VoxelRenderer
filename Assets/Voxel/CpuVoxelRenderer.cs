using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

public class CpuVoxelRenderer : MonoBehaviour
{
    public struct Voxel
    {
        public Vector3 position;
        public bool isRender;
    }

    const int voxelSizeOneLine = 10;

    [SerializeField, Range(0.1f, 1f)] float voxelScale = 0.25f;
    int voxelCountOneLine;


    [SerializeField] GameObject cloneObject;
    [SerializeField] GameObject sampleObject;

    [SerializeField] bool isSkiinedMesh = false;

    // [SerializeField] Vector3 v0 = new Vector3(-3, -4, 0.01f);
    // [SerializeField] Vector3 v1 = new Vector3(4, 4, 0);
    // [SerializeField] Vector3 v2 = new Vector3(3, -2, 0);

    Vector3 center;
    Vector3 size;
    Voxel[,,] voxels;
    bool[,,] renderBuffer;
    int offset;

    Vector3 a00, a01, a02, a10, a11, a12, a20, a21, a22;


    // Start is called before the first frame update
    void Start()
    {
        voxelInit();
        // culc();
        Render();
    }

    void voxelInit()
    {
        voxelCountOneLine = (int)(voxelSizeOneLine / voxelScale);

        voxels = new Voxel[voxelCountOneLine, voxelCountOneLine, voxelCountOneLine];
        renderBuffer = new bool[voxelCountOneLine, voxelCountOneLine, voxelCountOneLine];
        offset = (int)(voxelCountOneLine * 0.5f);

        for (int x = 0; x < voxelCountOneLine; x++)
        {
            for (int y = 0; y < voxelCountOneLine; y++)
            {
                for (int z = 0; z < voxelCountOneLine; z++)
                {
                    // Instantiate(cloneObject, new Vector3(x, y, z), quaternion.identity);
                    voxels[x, y, z] = new Voxel();
                    //positionはvoxelの左下
                    voxels[x, y, z].position = new Vector3((x - offset) * voxelScale, (y - offset) * voxelScale, (z - offset) * voxelScale);
                    voxels[x, y, z].isRender = false;

                    // renderBuffer[x, y, z] = true;
                }
            }
        }


        // //meshの頂点情報を取得
        Mesh mesh;
        if (isSkiinedMesh)
        {
            mesh = sampleObject.GetComponent<SkinnedMeshRenderer>().sharedMesh;
        }
        else
        {
            mesh = sampleObject.GetComponent<MeshFilter>().sharedMesh;
        }
        var indices = mesh.GetIndices(0);
        var vertices = mesh.vertices;
        // var normals = mesh.normals;
        var matrix = sampleObject.transform.localToWorldMatrix;

        for (int i = 0; i < indices.Length; i += 3)
        {
            var v0 = matrix.MultiplyPoint(vertices[indices[i]]);
            var v1 = matrix.MultiplyPoint(vertices[indices[i + 1]]);
            var v2 = matrix.MultiplyPoint(vertices[indices[i + 2]]);
            // Debug.Log($"{v0}  {v1}  {v2}");

            //ポリゴンごとのAABBを計算
            var min_x = Mathf.Min(v0.x, v1.x, v2.x);
            var min_y = Mathf.Min(v0.y, v1.y, v2.y);
            var min_z = Mathf.Min(v0.z, v1.z, v2.z);

            var max_x = Mathf.Max(v0.x, v1.x, v2.x);
            var max_y = Mathf.Max(v0.y, v1.y, v2.y);
            var max_z = Mathf.Max(v0.z, v1.z, v2.z);

            // Debug.Log($"porigon_aabb {min_x} {min_y} {min_z}   {max_x} {max_y} {max_z}");

            //ポリゴンのAABBとvoxelのAABBで交差しているvoxelのindexを計算
            var min_x_idx = Mathf.FloorToInt(min_x / voxelScale) + offset;
            var min_y_idx = Mathf.FloorToInt(min_y / voxelScale) + offset;
            var min_z_idx = Mathf.FloorToInt(min_z / voxelScale) + offset;

            var max_x_idx = Mathf.CeilToInt(max_x / voxelScale) + offset;
            var max_y_idx = Mathf.CeilToInt(max_y / voxelScale) + offset;
            var max_z_idx = Mathf.CeilToInt(max_z / voxelScale) + offset;

            // Debug.Log($"voxel_idx {min_x_idx} {min_y_idx} {min_z_idx}  {max_x_idx} {max_y_idx} {max_z_idx}");

            Vector3 f0 = v1 - v0;
            Vector3 f1 = v2 - v1;
            Vector3 f2 = v0 - v2;

            List<Vector3> axises = new List<Vector3>();
            //1.ポリゴンの3辺と、AABBの直交する3つの辺の組み合わせから得られる9個のクロス積
            a00 = Vector3.Cross(Vector3.right, f0);
            a01 = Vector3.Cross(Vector3.right, f1);
            a02 = Vector3.Cross(Vector3.right, f2);
            a10 = Vector3.Cross(Vector3.up, f0);
            a11 = Vector3.Cross(Vector3.up, f1);
            a12 = Vector3.Cross(Vector3.up, f2);
            a20 = Vector3.Cross(Vector3.forward, f0);
            a21 = Vector3.Cross(Vector3.forward, f1);
            a22 = Vector3.Cross(Vector3.forward, f2);


            axises.Add(a00);
            axises.Add(a01);
            axises.Add(a02);
            axises.Add(a10);
            axises.Add(a11);
            axises.Add(a12);
            axises.Add(a20);
            axises.Add(a21);
            axises.Add(a22);

            //2.AABB(voxel)の法線
            var b00 = Vector3.right;
            var b01 = Vector3.up;
            var b02 = Vector3.forward;

            axises.Add(b00);
            axises.Add(b01);
            axises.Add(b02);

            //3.ポリゴンの法線
            var c00 = Vector3.Cross(f0, f1).normalized;

            axises.Add(c00);


            List<Vector3> _vertices = new List<Vector3>();
            _vertices.Add(new Vector3(0, 0, 0));
            _vertices.Add(new Vector3(voxelScale, 0, 0));
            _vertices.Add(new Vector3(voxelScale, voxelScale, 0));
            _vertices.Add(new Vector3(0, voxelScale, 0));
            _vertices.Add(new Vector3(0, 0, voxelScale));
            _vertices.Add(new Vector3(voxelScale, 0, voxelScale));
            _vertices.Add(new Vector3(voxelScale, voxelScale, voxelScale));
            _vertices.Add(new Vector3(0, voxelScale, voxelScale));

            for (int x = min_x_idx; x < max_x_idx; x++)
            {
                for (int y = min_y_idx; y < max_y_idx; y++)
                {
                    for (int z = min_z_idx; z < max_z_idx; z++)
                    {
                        bool isIntersect = true;
                        for (int k = 0; k < axises.Count; k++)
                        {
                            //上記の軸に射影する
                            var a = Vector3.Dot(axises[k], v0);
                            var b = Vector3.Dot(axises[k], v1);
                            var c = Vector3.Dot(axises[k], v2);
                            var min = Mathf.Min(a, b, c);
                            var max = Mathf.Max(a, b, c);

                            // var vertices = cloneObject.GetComponent<MeshFilter>().sharedMesh.vertices;
                            List<float> dots = new List<float>();
                            for (int v = 0; v < 8; v++)
                            {
                                var d = Vector3.Dot(axises[k], _vertices[v] + new Vector3(x * voxelScale - voxelSizeOneLine * 0.5f, y * voxelScale - voxelSizeOneLine * 0.5f, z * voxelScale - voxelSizeOneLine * 0.5f));
                                dots.Add(d);
                            }
                            var v_min = Mathf.Min(dots.ToArray());
                            var v_max = Mathf.Max(dots.ToArray());
                            // var d = Vector3.Dot(a00, cloneObject.)

                            // if (v_min == v_max || min == max)
                            //     continue;

                            // Debug.Log($"{min} {max} {v_min} {v_max}");

                            //一つでも分離していたら
                            if (v_min > max || min > v_max)
                            {
                                // Debug.Log($"separate!  {min} {max} {v_min} {v_max}");
                                isIntersect = false;
                            }
                        }
                        if (isIntersect)
                            renderBuffer[x, y, z] = true;
                    }
                }
            }
        }


    }

    void OnDrawGizmos()
    {
        // Debug.DrawLine(v0, v1, Color.red);
        // Debug.DrawLine(v1, v2, Color.red);
        // Debug.DrawLine(v2, v0, Color.red);

        // Debug.DrawLine(Vector3.zero, a00, Color.yellow);
        // Debug.DrawLine(Vector3.zero, a01, Color.yellow);
        // Debug.DrawLine(Vector3.zero, a02, Color.yellow);
        // Debug.DrawLine(Vector3.zero, a00, Color.blue);

        Gizmos.color = new Color(0, 0, 1, 0.5F);
        Gizmos.DrawCube(center, size);
    }

    void Render()
    {
        for (int x = 0; x < voxelCountOneLine; x++)
        {
            for (int y = 0; y < voxelCountOneLine; y++)
            {
                for (int z = 0; z < voxelCountOneLine; z++)
                {
                    // Instantiate(cloneObject, new Vector3(x, y, z), quaternion.identity);
                    if (renderBuffer[x, y, z])
                    {
                        var obj = Instantiate(cloneObject, voxels[x, y, z].position + new Vector3(voxelScale * 0.5f, voxelScale * 0.5f, voxelScale * 0.5f), quaternion.identity);
                        obj.transform.localScale = Vector3.one * voxelScale;
                        obj.transform.parent = transform;
                    }
                }
            }
        }
    }
}
