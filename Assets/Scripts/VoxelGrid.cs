using UnityEngine;
using System.Collections.Generic;

[SelectionBase]
public class VoxelGrid : MonoBehaviour
{
    //part1 variables
    public int m_resolution;
    private float m_voxelSize, m_gridSize;

    private Voxel[] m_voxels;

    public GameObject voxelPrefab;
    private Material[] m_voxelMaterials;

    private Mesh m_mesh;

	private List<Vector3> m_vertices;
    private List<int> m_triangles;

    public VoxelGrid m_xNeighbor, m_yNeighbor, m_xyNeighbor;
    private Voxel m_dummyX, m_dummyY, m_dummyT;

    //part2 variables
    private int[] m_rowCacheMax, m_rowCacheMin;
    private int m_edgeCacheMin, m_edgeCacheMax;

    //part3 variables
    private float m_sharpFeatureLimit;

    public void Initialize(int resolution, float size, float maxFeatureAngle)
    {
        this.m_resolution = resolution;
        m_sharpFeatureLimit = Mathf.Cos(maxFeatureAngle * Mathf.Deg2Rad);
        m_gridSize = size;
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

        m_dummyX = new Voxel();
        m_dummyY = new Voxel();
        m_dummyT = new Voxel();


        //offset the grid to center it inside the screen
        Vector3 gridPosition = new Vector3(-0.5f * size, -0.5f * size, 0);
        this.transform.localPosition = gridPosition;

        GetComponent<MeshFilter>().mesh = m_mesh = new Mesh();
        m_mesh.name = "VoxelGrid Mesh";
        m_vertices = new List<Vector3>();
        m_triangles = new List<int>();
        m_rowCacheMax = new int[resolution * 2 + 1];
        m_rowCacheMin = new int[resolution * 2 + 1];
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
        int xStart = (int)(stencil.XStart / m_voxelSize);
        if (xStart < 0)
        {
            xStart = 0;
        }
        int xEnd = (int)(stencil.XEnd / m_voxelSize);
        if (xEnd >= m_resolution)
        {
            xEnd = m_resolution - 1;
        }
        int yStart = (int)(stencil.YStart / m_voxelSize);
        if (yStart < 0)
        {
            yStart = 0;
        }
        int yEnd = (int)(stencil.YEnd / m_voxelSize);
        if (yEnd >= m_resolution)
        {
            yEnd = m_resolution - 1;
        }

        for (int y = yStart; y <= yEnd; y++)
        {
            int i = y * m_resolution + xStart;
            for (int x = xStart; x <= xEnd; x++, i++)
            {
                stencil.Apply(m_voxels[i]);
            }
        }

        SetCrossings(stencil, xStart, xEnd, yStart, yEnd);
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

        if (m_xNeighbor != null)
        {
            m_dummyX.BecomeXDummyOf(m_xNeighbor.m_voxels[0], m_gridSize);
        }
        FillFirstRowCache();
        TriangulateCellRows();

        if (m_yNeighbor != null)
        {
            TriangulateGapRow();
        }

        m_mesh.vertices = m_vertices.ToArray();
        m_mesh.triangles = m_triangles.ToArray();
    }

    private void TriangulateCellRows()
    {
        int cells = m_resolution - 1;
        for (int i = 0, y = 0; y < cells; y++, i++)
        {
            SwapRowCaches();
            CacheFirstCorner(m_voxels[i + m_resolution]);
            CacheNextMiddleEdge(m_voxels[i], m_voxels[i + m_resolution]);

            for (int x = 0; x < cells; x++, i++)
            {
                Voxel
                       a = m_voxels[i],
                       b = m_voxels[i + 1],
                       c = m_voxels[i + m_resolution],
                       d = m_voxels[i + m_resolution + 1];
                int cacheIndex = x * 2;
                CacheNextEdgeAndCorner(cacheIndex, c, d);
                CacheNextMiddleEdge(b, d);
                TriangulateCell(cacheIndex, a, b, c, d);
            }

            if (m_xNeighbor != null)
            {
                //Debug.Log("TriangulateGapCell i:" + i + " y:" + y);
                TriangulateGapCell(i);
            }
        }
    }

    private void TriangulateGapCell(int i)
    {
        Voxel dummySwap = m_dummyT;
        dummySwap.BecomeXDummyOf(m_xNeighbor.m_voxels[i + 1], m_gridSize);
        m_dummyT = m_dummyX;
        m_dummyX = dummySwap;
        int cacheIndex = (m_resolution - 1) * 2;
        CacheNextEdgeAndCorner(cacheIndex, m_voxels[i + m_resolution], m_dummyX);
        CacheNextMiddleEdge(m_dummyT, m_dummyX);
        TriangulateCell(cacheIndex, m_voxels[i], m_dummyT, m_voxels[i + m_resolution], m_dummyX);
    }

    private void TriangulateGapRow()
    {
        m_dummyY.BecomeYDummyOf(m_yNeighbor.m_voxels[0], m_gridSize);
        int cells = m_resolution - 1;
        int offset = cells * m_resolution;
        SwapRowCaches();
        CacheFirstCorner(m_dummyY);
        CacheNextMiddleEdge(m_voxels[cells * m_resolution], m_dummyY);

        for (int x = 0; x < cells; x++)
        {
            Voxel dummySwap = m_dummyT;
            dummySwap.BecomeYDummyOf(m_yNeighbor.m_voxels[x + 1], m_gridSize);
            m_dummyT = m_dummyY;
            m_dummyY = dummySwap;
            int cacheIndex = x * 2;
            CacheNextEdgeAndCorner(cacheIndex, m_dummyT, m_dummyY);
            CacheNextMiddleEdge(m_voxels[x + offset + 1], m_dummyY);
            TriangulateCell(cacheIndex, m_voxels[x + offset], m_voxels[x + offset + 1], m_dummyT, m_dummyY);
        }

        if (m_xNeighbor != null)
        {
            m_dummyT.BecomeXYDummyOf(m_xyNeighbor.m_voxels[0], m_gridSize);
            int cacheIndex = cells * 2;
            CacheNextEdgeAndCorner(cacheIndex, m_dummyY, m_dummyT);
            CacheNextMiddleEdge(m_dummyX, m_dummyT);
            TriangulateCell(cacheIndex, m_voxels[m_voxels.Length - 1], m_dummyX, m_dummyY, m_dummyT);
        }
    }

    private void TriangulateCell(int i, Voxel a, Voxel b, Voxel c, Voxel d)
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
            case 0: TriangulateCase0(i, a, b, c, d); break;
            case 1: TriangulateCase1(i, a, b, c, d); break;
            case 2: TriangulateCase2(i, a, b, c, d); break;
            case 3: TriangulateCase3(i, a, b, c, d); break;
            case 4: TriangulateCase4(i, a, b, c, d); break;
            case 5: TriangulateCase5(i, a, b, c, d); break;
            case 6: TriangulateCase6(i, a, b, c, d); break;
            case 7: TriangulateCase7(i, a, b, c, d); break;
            case 8: TriangulateCase8(i, a, b, c, d); break;
            case 9: TriangulateCase9(i, a, b, c, d); break;
            case 10: TriangulateCase10(i, a, b, c, d); break;
            case 11: TriangulateCase11(i, a, b, c, d); break;
            case 12: TriangulateCase12(i, a, b, c, d); break;
            case 13: TriangulateCase13(i, a, b, c, d); break;
            case 14: TriangulateCase14(i, a, b, c, d); break;
            case 15: TriangulateCase15(i, a, b, c, d); break;
        }
    }

    private void TriangulateCase0(int i, Voxel a, Voxel b, Voxel c, Voxel d)
    {
    }

    private void TriangulateCase15(int i, Voxel a, Voxel b, Voxel c, Voxel d)
    {
        AddQuadABCD(i);
    }

    private void TriangulateCase1(int i, Voxel a, Voxel b, Voxel c, Voxel d)
    {
        Vector2 n1 = a.m_xNormal;
        Vector2 n2 = a.m_yNormal;
        if (IsSharpFeature(n1, n2))
        {
            Vector2 point = ComputeIntersection(a.XEdgePoint, n1, a.YEdgePoint, n2);
            if (ClampToCellMaxMax(ref point, a, d))
            {
                AddQuadA(i, point);
                return;
            }
        }
        AddTriangleA(i);
    }

    private void TriangulateCase2(int i, Voxel a, Voxel b, Voxel c, Voxel d)
    {
        Vector2 n1 = a.m_xNormal;
        Vector2 n2 = b.m_yNormal;
        if (IsSharpFeature(n1, n2))
        {
            Vector2 point = ComputeIntersection(a.XEdgePoint, n1, b.YEdgePoint, n2);
            if (ClampToCellMinMax(ref point, a, d))
            {
                AddQuadB(i, point);
                return;
            }
        }
        AddTriangleB(i);
    }

    private void TriangulateCase4(int i, Voxel a, Voxel b, Voxel c, Voxel d)
    {
        Vector2 n1 = c.m_xNormal;
        Vector2 n2 = a.m_yNormal;
        if (IsSharpFeature(n1, n2))
        {
            Vector2 point = ComputeIntersection(c.XEdgePoint, n1, a.YEdgePoint, n2);
            if (ClampToCellMaxMin(ref point, a, d))
            {
                AddQuadC(i, point);
                return;
            }
        }
        AddTriangleC(i);
    }

    private void TriangulateCase8(int i, Voxel a, Voxel b, Voxel c, Voxel d)
    {
        Vector2 n1 = c.m_xNormal;
        Vector2 n2 = b.m_yNormal;
        if (IsSharpFeature(n1, n2))
        {
            Vector2 point = ComputeIntersection(c.XEdgePoint, n1, b.YEdgePoint, n2);
            if (ClampToCellMinMin(ref point, a, d))
            {
                AddQuadD(i, point);
                return;
            }
        }
        AddTriangleD(i);
    }

    private void TriangulateCase7(int i, Voxel a, Voxel b, Voxel c, Voxel d)
    {
        Vector2 n1 = c.m_xNormal;
        Vector2 n2 = b.m_yNormal;
        if (IsSharpFeature(n1, n2))
        {
            Vector2 point = ComputeIntersection(c.XEdgePoint, n1, b.YEdgePoint, n2);
            if (IsInsideCell(point, a, d))
            {
                AddHexagonABC(i, point);
                return;
            }
        }
        AddPentagonABC(i);
    }

    private void TriangulateCase11(int i, Voxel a, Voxel b, Voxel c, Voxel d) {
		Vector2 n1 = c.m_xNormal;
		Vector2 n2 = a.m_yNormal;
		if (IsSharpFeature(n1, n2)) {
			Vector2 point = ComputeIntersection(c.XEdgePoint, n1, a.YEdgePoint, n2);
			if (IsInsideCell(point, a, d)) {
				AddHexagonABD(i, point);
				return;
			}
		}
		AddPentagonABD(i);
	}

    private void TriangulateCase13(int i, Voxel a, Voxel b, Voxel c, Voxel d) {
		Vector2 n1 = a.m_xNormal;
		Vector2 n2 = b.m_yNormal;
		if (IsSharpFeature(n1, n2)) {
			Vector2 point = ComputeIntersection(a.XEdgePoint, n1, b.YEdgePoint, n2);
			if (IsInsideCell(point, a, d)) {
				AddHexagonACD(i, point);
				return;
			}
		}
		AddPentagonACD(i);
	}

    private void TriangulateCase14(int i, Voxel a, Voxel b, Voxel c, Voxel d) {
		Vector2 n1 = a.m_xNormal;
		Vector2 n2 = a.m_yNormal;
		if (IsSharpFeature(n1, n2)) {
			Vector2 point = ComputeIntersection(a.XEdgePoint, n1, a.YEdgePoint, n2);
			if (IsInsideCell(point, a, d)) {
				AddHexagonBCD(i, point);
				return;
			}
		}
		AddPentagonBCD(i);
	}

    private void TriangulateCase3(int i, Voxel a, Voxel b, Voxel c, Voxel d)
    {
        AddQuadAB(i);
    }

    private void TriangulateCase5(int i, Voxel a, Voxel b, Voxel c, Voxel d)
    {
        AddQuadAC(i);
    }

    private void TriangulateCase10(int i, Voxel a, Voxel b, Voxel c, Voxel d)
    {
        AddQuadBD(i);
    }

    private void TriangulateCase12(int i, Voxel a, Voxel b, Voxel c, Voxel d)
    {
        AddQuadCD(i);
    }

    private void TriangulateCase6(int i, Voxel a, Voxel b, Voxel c, Voxel d)
    {
        AddTriangleB(i);
        AddTriangleC(i);
    }

    private void TriangulateCase9(int i, Voxel a, Voxel b, Voxel c, Voxel d)
    {
        AddTriangleA(i);
        AddTriangleD(i);
    }

    private void AddQuadABCD(int i)
    {
        AddQuad(m_rowCacheMin[i], m_rowCacheMax[i], m_rowCacheMax[i + 2], m_rowCacheMin[i + 2]);
    }

    private void AddTriangleA(int i)
    {
        AddTriangle(m_rowCacheMin[i], m_edgeCacheMin, m_rowCacheMin[i + 1]);
    }

    private void AddTriangleB(int i)
    {
        AddTriangle(m_rowCacheMin[i + 2], m_rowCacheMin[i + 1], m_edgeCacheMax);
    }

    private void AddTriangleC(int i)
    {
        AddTriangle(m_rowCacheMax[i], m_rowCacheMax[i + 1], m_edgeCacheMin);
    }

    private void AddTriangleD(int i)
    {
        AddTriangle(m_rowCacheMax[i + 2], m_edgeCacheMax, m_rowCacheMax[i + 1]);
    }

    private void AddPentagonABC(int i)
    {
        AddPentagon(m_rowCacheMin[i], m_rowCacheMax[i], m_rowCacheMax[i + 1], m_edgeCacheMax, m_rowCacheMin[i + 2]);
    }

    private void AddPentagonABD(int i)
    {
        AddPentagon(m_rowCacheMin[i + 2], m_rowCacheMin[i], m_edgeCacheMin, m_rowCacheMax[i + 1], m_rowCacheMax[i + 2]);
    }

    private void AddPentagonACD(int i)
    {
        AddPentagon(m_rowCacheMax[i], m_rowCacheMax[i + 2], m_edgeCacheMax, m_rowCacheMin[i + 1], m_rowCacheMin[i]);
    }

    private void AddPentagonBCD(int i)
    {
        AddPentagon(m_rowCacheMax[i + 2], m_rowCacheMin[i + 2], m_rowCacheMin[i + 1], m_edgeCacheMin, m_rowCacheMax[i]);
    }

    private void AddQuadAB(int i)
    {
        AddQuad(m_rowCacheMin[i], m_edgeCacheMin, m_edgeCacheMax, m_rowCacheMin[i + 2]);
    }

    private void AddQuadAC(int i)
    {
        AddQuad(m_rowCacheMin[i], m_rowCacheMax[i], m_rowCacheMax[i + 1], m_rowCacheMin[i + 1]);
    }

    private void AddQuadBD(int i)
    {
        AddQuad(m_rowCacheMin[i + 1], m_rowCacheMax[i + 1], m_rowCacheMax[i + 2], m_rowCacheMin[i + 2]);
    }

    private void AddQuadCD(int i)
    {
        AddQuad(m_edgeCacheMin, m_rowCacheMax[i], m_rowCacheMax[i + 2], m_edgeCacheMax);
    }

    private void AddQuadA(int i, Vector2 extraVertex)
    {
        AddQuad(m_vertices.Count, m_rowCacheMin[i + 1], m_rowCacheMin[i], m_edgeCacheMin);
        m_vertices.Add(extraVertex);
    }

    private void AddQuadB(int i, Vector2 extraVertex)
    {
        AddQuad(m_vertices.Count, m_edgeCacheMax, m_rowCacheMin[i + 2], m_rowCacheMin[i + 1]);
        m_vertices.Add(extraVertex);
    }

    private void AddQuadC(int i, Vector2 extraVertex)
    {
        AddQuad(m_vertices.Count, m_edgeCacheMin, m_rowCacheMax[i], m_rowCacheMax[i + 1]);
        m_vertices.Add(extraVertex);
    }

    private void AddQuadD(int i, Vector2 extraVertex)
    {
        AddQuad(m_vertices.Count, m_rowCacheMax[i + 1], m_rowCacheMax[i + 2], m_edgeCacheMax);
        m_vertices.Add(extraVertex);
    }

    private void AddHexagonABC(int i, Vector2 extraVertex)
    {
        AddHexagon(
            m_vertices.Count, m_edgeCacheMax, m_rowCacheMin[i + 2],
            m_rowCacheMin[i], m_rowCacheMax[i], m_rowCacheMax[i + 1]);
        m_vertices.Add(extraVertex);
    }

    private void AddHexagonABD(int i, Vector2 extraVertex)
    {
        AddHexagon(
            m_vertices.Count, m_rowCacheMax[i + 1], m_rowCacheMax[i + 2],
            m_rowCacheMin[i + 2], m_rowCacheMin[i], m_edgeCacheMin);
        m_vertices.Add(extraVertex);
    }

    private void AddHexagonACD(int i, Vector2 extraVertex)
    {
        AddHexagon(
            m_vertices.Count, m_rowCacheMin[i + 1], m_rowCacheMin[i],
            m_rowCacheMax[i], m_rowCacheMax[i + 2], m_edgeCacheMax);
        m_vertices.Add(extraVertex);
    }

    private void AddHexagonBCD(int i, Vector2 extraVertex)
    {
        AddHexagon(
            m_vertices.Count, m_edgeCacheMin, m_rowCacheMax[i],
            m_rowCacheMax[i + 2], m_rowCacheMin[i + 2], m_rowCacheMin[i + 1]);
        m_vertices.Add(extraVertex);
    }

    private void AddTriangle(int a, int b, int c)
    {
        m_triangles.Add(a);
        m_triangles.Add(b);
        m_triangles.Add(c);
    }

    private void AddQuad(int a, int b, int c, int d)
    {
        m_triangles.Add(a);
        m_triangles.Add(b);
        m_triangles.Add(c);
        m_triangles.Add(a);
        m_triangles.Add(c);
        m_triangles.Add(d);
    }

    private void AddPentagon(int a, int b, int c, int d, int e)
    {
        m_triangles.Add(a);
        m_triangles.Add(b);
        m_triangles.Add(c);
        m_triangles.Add(a);
        m_triangles.Add(c);
        m_triangles.Add(d);
        m_triangles.Add(a);
        m_triangles.Add(d);
        m_triangles.Add(e);
    }

    private void AddHexagon(int a, int b, int c, int d, int e, int f)
    {
        m_triangles.Add(a);
        m_triangles.Add(b);
        m_triangles.Add(c);
        m_triangles.Add(a);
        m_triangles.Add(c);
        m_triangles.Add(d);
        m_triangles.Add(a);
        m_triangles.Add(d);
        m_triangles.Add(e);
        m_triangles.Add(a);
        m_triangles.Add(e);
        m_triangles.Add(f);
    }
    
    /**
     * part 2 methods
     * **/
    private void FillFirstRowCache()
    {
        CacheFirstCorner(m_voxels[0]);
        int i;
        for (i = 0; i < m_resolution - 1; i++)
        {
            CacheNextEdgeAndCorner(i * 2, m_voxels[i], m_voxels[i + 1]);
        }

        //cache the gap cell
        if (m_xNeighbor != null)
        {
            m_dummyX.BecomeXDummyOf(m_xNeighbor.m_voxels[0], m_gridSize);
            CacheNextEdgeAndCorner(i * 2, m_voxels[i], m_dummyX);
        }
    }

    private void CacheFirstCorner(Voxel voxel)
    {
        if (voxel.m_state)
        {
            m_rowCacheMax[0] = m_vertices.Count;
            m_vertices.Add(voxel.m_position);
        }
    }
    
    private void CacheNextEdgeAndCorner(int i, Voxel xMin, Voxel xMax)
    {
        if (xMin.m_state != xMax.m_state)
        {
            m_rowCacheMax[i + 1] = m_vertices.Count;
            Vector3 p;
            p.x = xMin.m_xEdge;
            p.y = xMin.m_position.y;
            p.z = 0f;
            m_vertices.Add(p);
        }
        if (xMax.m_state)
        {
            m_rowCacheMax[i + 2] = m_vertices.Count;
            m_vertices.Add(xMax.m_position);
        }
    }

    private void SwapRowCaches()
    {
        int[] rowSwap = m_rowCacheMin;
        m_rowCacheMin = m_rowCacheMax;
        m_rowCacheMax = rowSwap;
    }

    private void CacheNextMiddleEdge(Voxel yMin, Voxel yMax)
    {
        m_edgeCacheMin = m_edgeCacheMax;
        if (yMin.m_state != yMax.m_state)
        {
            m_edgeCacheMax = m_vertices.Count;
            Vector3 p;
            p.x = yMin.m_position.x;
            p.y = yMin.m_yEdge;
            p.z = 0f;
            m_vertices.Add(p);
        }
    }

    private void SetCrossings(VoxelStencil stencil, int xStart, int xEnd, int yStart, int yEnd)
    {
        bool crossHorizontalGap = false;
        bool includeLastVerticalRow = false;
        bool crossVerticalGap = false;

        if (xStart > 0)
        {
            xStart -= 1;
        }
        if (xEnd == m_resolution - 1)
        {
            xEnd -= 1;
            crossHorizontalGap = m_xNeighbor != null;
        }
        if (yStart > 0)
        {
            yStart -= 1;
        }
        if (yEnd == m_resolution - 1)
        {
            yEnd -= 1;
            includeLastVerticalRow = true;
            crossVerticalGap = m_yNeighbor != null;
        }

        Voxel a, b;
        for (int y = yStart; y <= yEnd; y++)
        {
            int i = y * m_resolution + xStart;
            b = m_voxels[i];
            for (int x = xStart; x <= xEnd; x++, i++)
            {
                a = b;
                b = m_voxels[i + 1];
                stencil.SetVerticalCrossing(a, m_voxels[i + m_resolution]);
                stencil.SetHorizontalCrossing(a, b);
            }

            stencil.SetVerticalCrossing(b, m_voxels[i + m_resolution]);
            if (crossHorizontalGap)
            {
                m_dummyX.BecomeXDummyOf(m_xNeighbor.m_voxels[y * m_resolution], m_gridSize);
                stencil.SetHorizontalCrossing(b, m_dummyX);
            }
        }

        if (includeLastVerticalRow)
        {
            int i = m_voxels.Length - m_resolution + xStart;
            b = m_voxels[i];
            for (int x = xStart; x <= xEnd; x++, i++)
            {
                a = b;
                b = m_voxels[i + 1];
                stencil.SetHorizontalCrossing(a, b);
                if (crossVerticalGap)
                {
                    m_dummyY.BecomeYDummyOf(m_yNeighbor.m_voxels[x], m_gridSize);
                    stencil.SetVerticalCrossing(a, m_dummyY);
                }
            }
            if (crossVerticalGap)
            {
                m_dummyY.BecomeYDummyOf(m_yNeighbor.m_voxels[xEnd + 1], m_gridSize);
                stencil.SetVerticalCrossing(b, m_dummyY);
            }
            if (crossHorizontalGap)
            {
                m_dummyX.BecomeXDummyOf(m_xNeighbor.m_voxels[m_voxels.Length - m_resolution], m_gridSize);
                stencil.SetHorizontalCrossing(b, m_dummyX);
            }
        }
    }

    private bool IsSharpFeature(Vector2 n1, Vector2 n2)
    {
        float dot = Vector2.Dot(n1, -n2);
        return dot >= m_sharpFeatureLimit && dot < 0.9999f;
    }

    private static Vector2 ComputeIntersection(Vector2 p1, Vector2 n1, Vector2 p2, Vector2 n2)
    {
        Vector2 u2 = new Vector2(n2.y, -n2.x);
        float d2 = -Vector2.Dot(n1, p2 - p1) / Vector2.Dot(n1, u2);
        return p2 + d2 * u2;
    }

    private static bool ClampToCellMaxMax(ref Vector2 point, Voxel min, Voxel max)
    {
        if (point.x < min.m_position.x || point.y < min.m_position.y)
        {
            return false;
        }
        if (point.x > max.m_position.x)
        {
            point.x = max.m_position.x;
        }
        if (point.y > max.m_position.y)
        {
            point.y = max.m_position.y;
        }
        return true;
    }

    private static bool ClampToCellMinMin(ref Vector2 point, Voxel min, Voxel max)
    {
        if (point.x > max.m_position.x || point.y > max.m_position.y)
        {
            return false;
        }
        if (point.x < min.m_position.x)
        {
            point.x = min.m_position.x;
        }
        if (point.y < min.m_position.y)
        {
            point.y = min.m_position.y;
        }
        return true;
    }

    private static bool ClampToCellMinMax(ref Vector2 point, Voxel min, Voxel max)
    {
        if (point.x > max.m_position.x || point.y < min.m_position.y)
        {
            return false;
        }
        if (point.x < min.m_position.x)
        {
            point.x = min.m_position.x;
        }
        if (point.y > max.m_position.y)
        {
            point.y = max.m_position.y;
        }
        return true;
    }

    private static bool ClampToCellMaxMin(ref Vector2 point, Voxel min, Voxel max)
    {
        if (point.x < min.m_position.x || point.y > max.m_position.y)
        {
            return false;
        }
        if (point.x > max.m_position.x)
        {
            point.x = max.m_position.x;
        }
        if (point.y < min.m_position.y)
        {
            point.y = min.m_position.y;
        }
        return true;
    }

    private static bool IsInsideCell(Vector2 point, Voxel min, Voxel max)
    {
        return
            point.x > min.m_position.x && point.y > min.m_position.y &&
            point.x < max.m_position.x && point.y < max.m_position.y;
    }
}