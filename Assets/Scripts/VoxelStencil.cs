using UnityEngine;

public class VoxelStencil
{
    protected bool m_fillType;
    protected int m_centerX, m_centerY, m_radius;

    public int XStart
    {
        get
        {
            return m_centerX - m_radius;
        }
    }

    public int XEnd
    {
        get
        {
            return m_centerX + m_radius;
        }
    }

    public int YStart
    {
        get
        {
            return m_centerY - m_radius;
        }
    }

    public int YEnd
    {
        get
        {
            return m_centerY + m_radius;
        }
    }

    public virtual void Initialize(bool fillType, int radius)
    {
        this.m_fillType = fillType;
        this.m_radius = radius;
    }

    public virtual bool Apply(int x, int y, bool voxel)
    {
        return m_fillType;
    }

    public virtual void SetCenter(int x, int y)
    {
        m_centerX = x;
        m_centerY = y;
    }
}