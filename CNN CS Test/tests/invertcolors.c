// COPYRIGHT (C) UNKNOWN6656, 2016

__kernel void invert(__global char* source, __global char* dest, __global int* krnl, __global int* width)
{
	int id = get_global_id(0);

	// the arrays must be 32Bit BGRA-colors
	if (id % 4 != 3)
		dest[id] = 255 - source[id];
}
