using UnityEngine;

public class VoxelStencil
{
    protected bool m_fillType;
    protected float m_centerX, m_centerY, m_radius;

    public float XStart
    {
        get
        {
            return m_centerX - m_radius;
        }
    }

    public float XEnd
    {
        get
        {
            return m_centerX + m_radius;
        }
    }

    public float YStart
    {
        get
        {
            return m_centerY - m_radius;
        }
    }

    public float YEnd
    {
        get
        {
            return m_centerY + m_radius;
        }
    }

    public virtual void Initialize(bool fillType, float radius)
    {
        this.m_fillType = fillType;
        this.m_radius = radius;
    }

    public virtual void Apply(Voxel voxel)
    {
        Vector2 p = voxel.m_position;
        if (p.x >= XStart && p.x <= XEnd && p.y >= YStart && p.y <= YEnd)
        {
            voxel.m_state = m_fillType;
        }
    }

    public virtual void SetCenter(float x, float y)
    {
        m_centerX = x;
        m_centerY = y;
    }
}