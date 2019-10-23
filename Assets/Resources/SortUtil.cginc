cbuffer grid {
	float3 _GridDim;
	float _GridH;
};

StructuredBuffer<uint2> _GridIndicesBufferRead;
RWStructuredBuffer<uint2> _GridIndicesBufferWrite;

float3 GridCalculateCell(float3 pos) {
	return pos / _GridH;
}

uint GridKey(uint3 xyz) {
	return xyz.x + xyz.y * _GridDim.x + xyz.z * _GridDim.x * _GridDim.y;
}

uint2 MakeKeyValuePair(uint3 xyz, uint value) {
    return uint2(GridKey(xyz), value); // uint2([GridHash], [ParticleID])
}

uint GridGetKey(uint2 pair) {
	return pair.x;
}

uint GridGetValue(uint2 pair) {
	return pair.y;
}
