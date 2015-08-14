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
}
