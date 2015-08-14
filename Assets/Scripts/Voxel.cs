using UnityEngine;
using System;

[Serializable]
public class Voxel
{
    public bool m_state;

    public Vector2 m_position;

    public float m_xEdge, m_yEdge;

    public Vector2 m_xNormal, m_yNormal;

    public Voxel() 
    {
        
    }

    public Voxel(int x, int y, float size)
    {
        m_position.x = (x + 0.5f) * size;
        m_position.y = (y + 0.5f) * size;

        m_xEdge = float.MinValue;
        m_yEdge = float.MinValue;
    }

    public void BecomeXDummyOf(Voxel voxel, float offset)
    {
        m_state = voxel.m_state;
        m_position = voxel.m_position;
        m_position.x += offset;
        m_xEdge = voxel.m_xEdge + offset;
        m_yEdge = voxel.m_yEdge;
        m_yNormal = voxel.m_yNormal;
    }

    public void BecomeYDummyOf(Voxel voxel, float offset)
    {
        m_state = voxel.m_state;
        m_position = voxel.m_position;
        m_position.y += offset;
        m_xEdge = voxel.m_xEdge;
        m_yEdge = voxel.m_yEdge + offset;
        m_xNormal = voxel.m_xNormal;
    }

    public void BecomeXYDummyOf(Voxel voxel, float offset)
    {
        m_state = voxel.m_state;
        m_position = voxel.m_position;
        m_position.x += offset;
        m_position.y += offset;
        m_xEdge = voxel.m_xEdge + offset;
        m_yEdge = voxel.m_yEdge + offset;
    }
}