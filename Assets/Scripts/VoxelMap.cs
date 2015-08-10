using UnityEngine;

public class VoxelMap : MonoBehaviour
{
    public float m_size = 640.0f;

    public int m_voxelResolution = 8;
    public int m_chunkResolution = 2;

    public GameObject m_voxelGridPrefab;

    private VoxelGrid[] m_chunks;

    private float m_chunkSize, m_voxelSize, m_halfSize;

    private static string[] m_fillTypeNames = { "Filled", "Empty" };
    private static string[] m_radiusNames = { "0", "1", "2", "3", "4", "5" };
    private static string[] stencilNames = { "Square", "Circle" };

    private int m_fillTypeIndex, m_radiusIndex, m_stencilIndex;

    private VoxelStencil[] stencils = {
		new VoxelStencil(),
		new VoxelStencilCircle()
	};

    private void Awake()
    {
        m_halfSize = m_size * 0.5f;
        m_chunkSize = m_size / m_chunkResolution;
        m_voxelSize = m_chunkSize / m_voxelResolution;

        m_chunks = new VoxelGrid[m_chunkResolution * m_chunkResolution];
        for (int i = 0, y = 0; y < m_chunkResolution; y++)
        {
            for (int x = 0; x < m_chunkResolution; x++, i++)
            {
                CreateChunk(i, x, y);
            }
        }

        BoxCollider box = gameObject.AddComponent<BoxCollider>();
        box.size = new Vector3(m_size, m_size);
    }

    private void CreateChunk(int i, int x, int y)
    {
        GameObject chunkObject = Instantiate(m_voxelGridPrefab);
        VoxelGrid chunk = chunkObject.GetComponent<VoxelGrid>();
        chunk.Initialize(m_voxelResolution, m_chunkSize);
        chunk.transform.parent = transform;
        chunk.transform.localPosition = new Vector3(x * m_chunkSize - m_halfSize, y * m_chunkSize - m_halfSize);
        m_chunks[i] = chunk;
    }

    private void EditVoxels(Vector3 point)
    {
        int centerX = (int)((point.x + m_halfSize) / m_voxelSize);
        int centerY = (int)((point.y + m_halfSize) / m_voxelSize);
        int chunkX = centerX / m_voxelResolution;
        int chunkY = centerY / m_voxelResolution;

        int xStart = (centerX - m_radiusIndex) / m_voxelResolution;
        if (xStart < 0)
        {
            xStart = 0;
        }
        int xEnd = (centerX + m_radiusIndex) / m_voxelResolution;
        if (xEnd >= m_chunkResolution)
        {
            xEnd = m_chunkResolution - 1;
        }
        int yStart = (centerY - m_radiusIndex) / m_voxelResolution;
        if (yStart < 0)
        {
            yStart = 0;
        }
        int yEnd = (centerY + m_radiusIndex) / m_voxelResolution;
        if (yEnd >= m_chunkResolution)
        {
            yEnd = m_chunkResolution - 1;
        }

        VoxelStencil activeStencil = stencils[m_stencilIndex];
        activeStencil.Initialize(m_fillTypeIndex == 0, m_radiusIndex);

        int voxelYOffset = yStart * m_voxelResolution;
        for (int y = yStart; y <= yEnd; y++)
        {
            int i = y * m_chunkResolution + xStart;
            int voxelXOffset = xStart * m_voxelResolution;
            for (int x = xStart; x <= xEnd; x++, i++)
            {
                activeStencil.SetCenter(centerX - voxelXOffset, centerY - voxelYOffset);
                m_chunks[i].Apply(activeStencil);
                voxelXOffset += m_voxelResolution;
            }
            voxelYOffset += m_voxelResolution;
        }
    }

    private void Update()
    {
        if (Input.GetMouseButton(0))
        {
            RaycastHit hitInfo;
            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hitInfo))
            {
                if (hitInfo.collider.gameObject == gameObject)
                {
                    EditVoxels(transform.InverseTransformPoint(hitInfo.point));
                }
            }
        }
    }

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(4f, 4f, 150f, 500f));
        GUILayout.Label("Fill Type");
        m_fillTypeIndex = GUILayout.SelectionGrid(m_fillTypeIndex, m_fillTypeNames, 2);
        GUILayout.Label("Radius");
        m_radiusIndex = GUILayout.SelectionGrid(m_radiusIndex, m_radiusNames, 6);
        GUILayout.Label("Stencil");
        m_stencilIndex = GUILayout.SelectionGrid(m_stencilIndex, stencilNames, 2);
        GUILayout.EndArea();
    }
}