using UnityEngine;
using System;

[Serializable]
public class Voxel
{
    public bool m_state;

    public Vector2 m_position, m_xEdgePosition, m_yEdgePosition;

    public Voxel() { }

    public Voxel(int x, int y, float size)
    {
        m_position.x = (x + 0.5f) * size;
        m_position.y = (y + 0.5f) * size;

        m_xEdgePosition = m_position;
        m_xEdgePosition.x += size * 0.5f;
        m_yEdgePosition = m_position;
        m_yEdgePosition.y += size * 0.5f;
    }

    public void BecomeXDummyOf(Voxel voxel, float offset)
    {
        m_state = voxel.m_state;
        m_position = voxel.m_position;
        m_xEdgePosition = voxel.m_xEdgePosition;
        m_yEdgePosition = voxel.m_yEdgePosition;
        m_position.x += offset;
        m_xEdgePosition.x += offset;
        m_yEdgePosition.x += offset;
    }

    public void BecomeYDummyOf(Voxel voxel, float offset)
    {
        m_state = voxel.m_state;
        m_position = voxel.m_position;
        m_xEdgePosition = voxel.m_xEdgePosition;
        m_yEdgePosition = voxel.m_yEdgePosition;
        m_position.y += offset;
        m_xEdgePosition.y += offset;
        m_yEdgePosition.y += offset;
    }

    public void BecomeXYDummyOf(Voxel voxel, float offset)
    {
        m_state = voxel.m_state;
        m_position = voxel.m_position;
        m_xEdgePosition = voxel.m_xEdgePosition;
        m_yEdgePosition = voxel.m_yEdgePosition;
        m_position.x += offset;
        m_position.y += offset;
        m_xEdgePosition.x += offset;
        m_xEdgePosition.y += offset;
        m_yEdgePosition.x += offset;
        m_yEdgePosition.y += offset;
    }
}