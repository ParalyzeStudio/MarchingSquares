using UnityEngine;
using System.Collections.Generic;

[SelectionBase]
public class VoxelGrid : MonoBehaviour
{
    public int m_resolution;
    private float m_voxelSize;

    private Voxel[] m_voxels;

    public GameObject voxelPrefab;
    private Material[] m_voxelMaterials;

    private Mesh m_mesh;

	private List<Vector3> m_vertices;
    private List<int> m_triangles;

    public void Initialize(int resolution, float size)
    {
        this.m_resolution = resolution;
        m_voxelSize = size / resolution;
        m_voxels = new Voxel[resolution * resolution];
        m_voxelMaterials = new Material[m_voxels.Length];

        for (int i = 0, y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++, i++)
            {
                CreateVoxel(i, x, y);
            }
        }

        //offset the grid to center it inside the screen
        Vector3 gridPosition = new Vector3(-0.5f * size, -0.5f * size, 0);
        this.transform.localPosition = gridPosition;

        GetComponent<MeshFilter>().mesh = m_mesh = new Mesh();
        m_mesh.name = "VoxelGrid Mesh";
        m_vertices = new List<Vector3>();
        m_triangles = new List<int>();
        Refresh();
    }

    private void CreateVoxel(int i, int x, int y)
    {
        GameObject o = Instantiate(voxelPrefab) as GameObject;
        o.transform.parent = transform;
        o.transform.localPosition = new Vector3((x + 0.5f) * m_voxelSize, (y + 0.5f) * m_voxelSize);
        o.transform.localScale = Vector3.one * m_voxelSize * 0.1f;
        m_voxelMaterials[i] = o.GetComponent<MeshRenderer>().material;
        m_voxels[i] = new Voxel(x, y, m_voxelSize);
    }


    public void Apply(VoxelStencil stencil)
    {
        int xStart = stencil.XStart;
        int xEnd = stencil.XEnd;
        int yStart = stencil.YStart;
        int yEnd = stencil.YEnd;

        if (xStart < 0)
        {
            xStart = 0;
        }
        if (xEnd >= m_resolution)
        {
            xEnd = m_resolution - 1;
        }
        if (yStart < 0)
        {
            yStart = 0;
        }
        if (yEnd >= m_resolution)
        {
            yEnd = m_resolution - 1;
        }

        for (int y = yStart; y <= yEnd; y++)
        {
            int i = y * m_resolution + xStart;
            for (int x = xStart; x <= xEnd; x++, i++)
            {
                m_voxels[i].m_state = stencil.Apply(x, y, m_voxels[i].m_state);
            }
        }

        Refresh();
    }

    private void SetVoxelColors()
    {
        for (int i = 0; i < m_voxels.Length; i++)
        {
            m_voxelMaterials[i].color = m_voxels[i].m_state ? Color.black : Color.white;
        }
    }

    private void Refresh()
    {
        SetVoxelColors();
        Triangulate();
    }

    private void Triangulate()
    {
        m_vertices.Clear();
        m_triangles.Clear();
        m_mesh.Clear();

        TriangulateCellRows();

        m_mesh.vertices = m_vertices.ToArray();
        m_mesh.triangles = m_triangles.ToArray();
    }

    private void TriangulateCellRows()
    {
        int cells = m_resolution - 1;
        for (int i = 0, y = 0; y < cells; y++, i++)
        {
            for (int x = 0; x < cells; x++, i++)
            {
                TriangulateCell(
                    m_voxels[i],
                    m_voxels[i + 1],
                    m_voxels[i + m_resolution],
                    m_voxels[i + m_resolution + 1]);
            }
        }
    }

    private void TriangulateCell(Voxel a, Voxel b, Voxel c, Voxel d)
    {
        int cellType = 0;
        if (a.m_state)
        {
            cellType |= 1;
        }
        if (b.m_state)
        {
            cellType |= 2;
        }
        if (c.m_state)
        {
            cellType |= 4;
        }
        if (d.m_state)
        {
            cellType |= 8;
        }

        switch (cellType)
        {
            case 0:
                return;
            case 1:
                AddTriangle(a.m_position, a.m_yEdgePosition, a.m_xEdgePosition);
                break;
            case 2:
                AddTriangle(b.m_position, a.m_xEdgePosition, b.m_yEdgePosition);
                break;
            case 3:
                AddQuad(a.m_position, a.m_yEdgePosition, b.m_yEdgePosition, b.m_position);
                break;
            case 4:
                AddTriangle(c.m_position, c.m_xEdgePosition, a.m_yEdgePosition);
                break;
            case 5:
                AddQuad(c.m_position, c.m_xEdgePosition, a.m_xEdgePosition, a.m_xEdgePosition);
                break;
            case 6:
                AddTriangle(b.m_position, a.m_xEdgePosition, b.m_yEdgePosition);
                AddTriangle(c.m_position, c.m_xEdgePosition, a.m_yEdgePosition);
                break;
            case 7:
                AddPentagon(a.m_position, c.m_position, c.m_xEdgePosition, b.m_yEdgePosition, b.m_position);
                break;
            case 8:
                AddTriangle(d.m_position, b.m_yEdgePosition, c.m_xEdgePosition);
                break;
            case 9:
                AddTriangle(a.m_position, a.m_yEdgePosition, a.m_xEdgePosition);
                AddTriangle(d.m_position, b.m_yEdgePosition, c.m_xEdgePosition);
                break;
            case 10:
                AddQuad(c.m_position, d.m_position, b.m_yEdgePosition, a.m_yEdgePosition);
                break;
            case 11:
                AddPentagon(b.m_position, a.m_position, a.m_yEdgePosition, c.m_xEdgePosition, d.m_position);
                break;
            case 12:
                AddQuad(c.m_xEdgePosition, d.m_position, b.m_position, a.m_xEdgePosition);
                break;
            case 13:
                AddPentagon(c.m_position, d.m_position, b.m_yEdgePosition, a.m_xEdgePosition, a.m_position);
                break;
            case 14:
                AddPentagon(d.m_position, b.m_position, a.m_xEdgePosition, a.m_yEdgePosition, c.m_position);
                break;
            case 15:
                AddQuad(a.m_position, c.m_position, d.m_position, b.m_position);
			    break;
        }
    }

    private void AddTriangle(Vector3 a, Vector3 b, Vector3 c)
    {
        int vertexIndex = m_vertices.Count;
        m_vertices.Add(a);
        m_vertices.Add(b);
        m_vertices.Add(c);
        m_triangles.Add(vertexIndex);
        m_triangles.Add(vertexIndex + 1);
        m_triangles.Add(vertexIndex + 2);
    }

    private void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
        int vertexIndex = m_vertices.Count;
        m_vertices.Add(a);
        m_vertices.Add(b);
        m_vertices.Add(c);
        m_vertices.Add(d);
        m_triangles.Add(vertexIndex);
        m_triangles.Add(vertexIndex + 1);
        m_triangles.Add(vertexIndex + 2);
        m_triangles.Add(vertexIndex);
        m_triangles.Add(vertexIndex + 2);
        m_triangles.Add(vertexIndex + 3);
    }

    private void AddPentagon(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 e)
    {
        int vertexIndex = m_vertices.Count;
        m_vertices.Add(a);
        m_vertices.Add(b);
        m_vertices.Add(c);
        m_vertices.Add(d);
        m_vertices.Add(e);
        m_triangles.Add(vertexIndex);
        m_triangles.Add(vertexIndex + 1);
        m_triangles.Add(vertexIndex + 2);
        m_triangles.Add(vertexIndex);
        m_triangles.Add(vertexIndex + 2);
        m_triangles.Add(vertexIndex + 3);
        m_triangles.Add(vertexIndex);
        m_triangles.Add(vertexIndex + 3);
        m_triangles.Add(vertexIndex + 4);
    }
}