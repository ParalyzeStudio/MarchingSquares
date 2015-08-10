using UnityEngine;
using System;

[Serializable]
public class Voxel
{
    public bool m_state;

    public Vector2 m_position, m_xEdgePosition, m_yEdgePosition;

    public Voxel(int x, int y, float size)
    {
        m_position.x = (x + 0.5f) * size;
        m_position.y = (y + 0.5f) * size;

        m_xEdgePosition = m_position;
        m_xEdgePosition.x += size * 0.5f;
        m_yEdgePosition = m_position;
        m_yEdgePosition.y += size * 0.5f;
    }
}