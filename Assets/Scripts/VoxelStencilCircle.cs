using UnityEngine;

public class VoxelStencilCircle : VoxelStencil {
	
	private int m_sqrRadius;
	
	public override void Initialize (bool fillType, int radius) {
		base.Initialize (fillType, radius);
        m_sqrRadius = radius * radius;
	}
	
	public override bool Apply (int x, int y, bool voxel) {
		x -= m_centerX;
		y -= m_centerY;
        if (x * x + y * y <= m_sqrRadius)
        {
			return m_fillType;
		}
		return voxel;
	}
}
