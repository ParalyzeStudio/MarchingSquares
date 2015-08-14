using UnityEngine;

public class VoxelStencilCircle : VoxelStencil {
	
	private float m_sqrRadius;

    public override void Initialize(bool fillType, float radius)
    {
		base.Initialize (fillType, radius);
        m_sqrRadius = radius * radius;
	}
	
	public override void Apply (Voxel voxel)
    {
        float x = voxel.m_position.x - m_centerX;
        float y = voxel.m_position.y - m_centerY;
        if (x * x + y * y <= m_sqrRadius)
        {
            voxel.m_state = m_fillType;
        }
	}

    protected override void FindHorizontalCrossing(Voxel xMin, Voxel xMax)
    {
        float y2 = xMin.m_position.y - m_centerY;
        y2 *= y2;
        if (xMin.m_state == m_fillType)
        {
            float x = xMin.m_position.x - m_centerX;
            if (x * x + y2 <= m_sqrRadius)
            {
                x = m_centerX + Mathf.Sqrt(m_sqrRadius - y2);
                if (xMin.m_xEdge == float.MinValue || xMin.m_xEdge < x)
                {
                    xMin.m_xEdge = x;
                }
            }
        }
        else if (xMax.m_state == m_fillType)
        {
            float x = xMax.m_position.x - m_centerX;
            if (x * x + y2 <= m_sqrRadius)
            {
                x = m_centerX - Mathf.Sqrt(m_sqrRadius - y2);
                if (xMin.m_xEdge == float.MinValue || xMin.m_xEdge > x)
                {
                    xMin.m_xEdge = x;
                }
            }
        }
    }

    protected override void FindVerticalCrossing(Voxel yMin, Voxel yMax)
    {
        float x2 = yMin.m_position.x - m_centerX;
        x2 *= x2;
        if (yMin.m_state == m_fillType)
        {
            float y = yMin.m_position.y - m_centerY;
            if (y * y + x2 <= m_sqrRadius)
            {
                y = m_centerY + Mathf.Sqrt(m_sqrRadius - x2);
                if (yMin.m_yEdge == float.MinValue || yMin.m_yEdge < y)
                {
                    yMin.m_yEdge = y;
                }
            }
        }
        else if (yMax.m_state == m_fillType)
        {
            float y = yMax.m_position.y - m_centerY;
            if (y * y + x2 <= m_sqrRadius)
            {
                y = m_centerY - Mathf.Sqrt(m_sqrRadius - x2);
                if (yMin.m_yEdge == float.MinValue || yMin.m_yEdge > y)
                {
                    yMin.m_yEdge = y;
                }
            }
        }
    }
}
