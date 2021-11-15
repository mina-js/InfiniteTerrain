using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class MeshGenerator : MonoBehaviour
{
    Mesh mesh;
    Vector3[] vertices;
    int[] triangles;

    public int xSize = 20;
    public int zSize = 20;

    // Start is called before the first frame update
    void Start()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        StartCoroutine(CreateShape());
    }

    private void Update()
    {
        UpdateMesh();
    }

    IEnumerator CreateShape()
    {
        vertices = new Vector3[(xSize + 1) * (zSize + 1)];

        int i = 0;

        for(int z = 0; z <= zSize; z++)
        {
            for(int x = 0; x <= xSize; x++)
            {
                float y = Mathf.PerlinNoise(x * .3f, z * .3f) * 2f;
                vertices[i] = new Vector3(x, y, z);
                i++;
            }
        }

        int vert = 0;
        int tri = 0;

        triangles = new int[6 * xSize * zSize];

        for(int z = 0; z < xSize; z++)
        {
            for (int x = 0; x < xSize; x++)
            {
                triangles[tri + 0] = vert;
                triangles[tri + 1] = vert + xSize + 1;
                triangles[tri + 2] = vert + 1;
                triangles[tri + 3] = vert + 1;
                triangles[tri + 4] = vert + xSize + 1;
                triangles[tri + 5] = vert + xSize + 2;
                vert++;
                tri += 6;

                yield return new WaitForSeconds(.01f);
            }

            vert++;
        }


    }

    void UpdateMesh()
    {
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
    }

    private void OnDrawGizmos()
    {
        if (vertices.Length == 0) return;
        for(int i = 0; i < vertices.Length; i++)
        {
            Gizmos.DrawSphere(vertices[i], .1f);
        }
    }
}
