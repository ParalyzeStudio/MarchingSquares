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

    public void SetHorizontalCrossing(Voxel xMin, Voxel xMax)
    {
        if (xMin.m_state != xMax.m_state)
        {
            FindHorizontalCrossing(xMin, xMax);
        }
    }

    public void SetVerticalCrossing(Voxel yMin, Voxel yMax)
    {
        if (yMin.m_state != yMax.m_state)
        {
            FindVerticalCrossing(yMin, yMax);
        }
    }

    protected virtual void FindHorizontalCrossing(Voxel xMin, Voxel xMax)
    {
        if (xMin.m_position.y < YStart || xMin.m_position.y > YEnd)
        {
            return;
        }

        if (xMin.m_state == m_fillType)
        {
            if (xMin.m_position.x <= XEnd && xMax.m_position.x >= XEnd)
            {
                xMin.m_xEdge = XEnd;
            }
        }
        else if (xMax.m_state == m_fillType)
        {
            if (xMin.m_position.x <= XStart && xMax.m_position.x >= XStart)
            {
                xMin.m_xEdge = XStart;
            }
        }
    }

    protected virtual void FindVerticalCrossing(Voxel yMin, Voxel yMax)
    {
        if (yMin.m_position.x < XStart || yMin.m_position.x > XEnd)
        {
            return;
        }
        if (yMin.m_state == m_fillType)
        {
            if (yMin.m_position.y <= YEnd && yMax.m_position.y >= YEnd)
            {
                yMin.m_yEdge = YEnd;
            }
        }
        else if (yMax.m_state == m_fillType)
        {
            if (yMin.m_position.y <= YStart && yMax.m_position.y >= YStart)
            {
                yMin.m_yEdge = YStart;
            }
        }
    }
}